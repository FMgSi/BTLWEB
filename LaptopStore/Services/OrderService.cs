using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Message, OrderResponseDto? Order)> CreateOrderAsync(CreateOrderRequest req, int? userId = null)
    {
        if (req.Items == null || !req.Items.Any())
        {
            return (false, "Giỏ hàng rỗng, không thể tạo đơn hàng.", null);
        }

        // Bọc trong CreateExecutionStrategy để tương thích hoàn toàn với cấu hình EnableRetryOnFailure của SQL Server
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<(bool Success, string Message, OrderResponseDto? Order)>(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var productIds = req.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _db.Products
                    .Include(p => p.Specification)
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                decimal subTotal = 0;
                var orderItems = new List<OrderItem>();

                foreach (var itemReq in req.Items)
                {
                    if (!products.TryGetValue(itemReq.ProductId, out var product))
                    {
                        await tx.RollbackAsync();
                        return (false, $"Sản phẩm mã #{itemReq.ProductId} không tồn tại trên hệ thống.", null);
                    }

                    if (!product.IsActive)
                    {
                        await tx.RollbackAsync();
                        return (false, $"Sản phẩm '{product.ProductName}' hiện đã ngừng kinh doanh.", null);
                    }

                    // CHỐNG BÁN VƯỢT TỒN KHO (CONCURRENCY SAFE):
                    // Thực thi UPDATE nguyên tử có điều kiện trực tiếp dưới SQL Server với kiểm tra số dòng bị ảnh hưởng
                    int rowsAffected;
                    if (_db.Database.IsRelational())
                    {
                        rowsAffected = await _db.Products
                            .Where(p => p.ProductId == itemReq.ProductId && p.IsActive && p.StockQuantity >= itemReq.Quantity)
                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - itemReq.Quantity));
                    }
                    else
                    {
                        // Fallback cho môi trường test InMemory (không hỗ trợ ExecuteUpdateAsync quan hệ)
                        if (product.StockQuantity >= itemReq.Quantity)
                        {
                            product.StockQuantity -= itemReq.Quantity;
                            await _db.SaveChangesAsync();
                            rowsAffected = 1;
                        }
                        else
                        {
                            rowsAffected = 0;
                        }
                    }

                    if (rowsAffected == 0)
                    {
                        await tx.RollbackAsync();
                        var freshP = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == itemReq.ProductId);
                        int currentStock = freshP?.StockQuantity ?? 0;
                        return (false, $"Sản phẩm '{product.ProductName}' không đủ tồn kho (chỉ còn {currentStock} máy). Vui lòng điều chỉnh giỏ hàng!", null);
                    }

                    // Chụp lại giá và cấu hình tại thời điểm mua (Phi chuẩn hóa kế toán có chủ đích)
                    decimal unitPrice = product.DiscountPrice ?? product.Price;
                    decimal itemTotal = unitPrice * itemReq.Quantity;
                    subTotal += itemTotal;

                    string specSnapshot = itemReq.SelectedSpecs ?? "";
                    if (string.IsNullOrWhiteSpace(specSnapshot) && product.Specification != null)
                    {
                        specSnapshot = $"RAM {product.Specification.RamGB}GB • {product.Specification.StorageGB}GB {product.Specification.StorageType}";
                    }

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        SelectedSpecs = specSnapshot,
                        ThumbnailUrl = product.ThumbnailUrl,
                        UnitPrice = unitPrice,
                        Quantity = itemReq.Quantity,
                        TotalPrice = itemTotal
                    });
                }

                // Sinh mã đơn hàng duy nhất: PC + YYMMDD + số ngẫu nhiên 4 chữ số
                string orderCode;
                int attempts = 0;
                do
                {
                    orderCode = $"PC{DateTime.Now:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
                    attempts++;
                } while (await _db.Orders.AnyAsync(o => o.OrderCode == orderCode) && attempts < 10);

                string initialPaymentStatus = req.PaymentMethod.Contains("VietQR", StringComparison.OrdinalIgnoreCase) ||
                                              req.PaymentMethod.Contains("Banking", StringComparison.OrdinalIgnoreCase)
                                              ? "Chờ xác nhận"
                                              : "Chờ thanh toán";

                string initialOrderStatus = "Đang xử lý";

                var order = new Order
                {
                    OrderCode = orderCode,
                    UserId = userId,
                    CustomerName = req.CustomerName.Trim(),
                    PhoneNumber = req.PhoneNumber.Trim(),
                    Email = req.Email?.Trim(),
                    ShippingAddress = req.ShippingAddress.Trim(),
                    Notes = req.Notes?.Trim(),
                    SubTotal = subTotal,
                    DiscountAmount = 0,
                    TotalAmount = subTotal,
                    PaymentMethod = req.PaymentMethod,
                    PaymentStatus = initialPaymentStatus,
                    OrderStatus = initialOrderStatus,
                    CreatedAt = DateTime.UtcNow,
                    OrderItems = orderItems
                };

                order.Payments.Add(new Payment
                {
                    PaymentMethod = req.PaymentMethod,
                    Amount = subTotal,
                    PaymentStatus = initialPaymentStatus,
                    PaymentDate = DateTime.UtcNow,
                    Note = $"Khởi tạo giao dịch {req.PaymentMethod}"
                });

                order.StatusHistories.Add(new OrderStatusHistory
                {
                    Status = initialOrderStatus,
                    ChangedBy = "Hệ thống",
                    ChangedAt = DateTime.UtcNow,
                    Note = "Khách hàng tạo đơn đặt hàng trực tuyến"
                });

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                var resDto = MapToResponseDto(order);
                return (true, "Đặt hàng thành công!", resDto);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, "Lỗi khi lưu đơn hàng: " + ex.Message, null);
            }
        });
    }

    public async Task<PagedResult<OrderResponseDto>> GetOrdersAsync(string? search, string? status, int page, int pageSize)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o =>
                o.OrderCode.ToLower().Contains(s) ||
                o.CustomerName.ToLower().Contains(s) ||
                o.PhoneNumber.Contains(s) ||
                (o.Email != null && o.Email.ToLower().Contains(s))
            );
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            query = query.Where(o => o.OrderStatus.ToLower() == status.ToLower() || o.PaymentStatus.ToLower() == status.ToLower());
        }

        var total = await query.CountAsync();
        var rawOrders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<OrderResponseDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = rawOrders.Select(MapToResponseDto).ToList()
        };
    }

    public async Task<OrderResponseDto?> GetOrderByCodeAsync(string orderCode)
    {
        var o = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode.Trim());

        return o == null ? null : MapToResponseDto(o);
    }

    public async Task<List<OrderResponseDto>> GetUserOrdersAsync(int userId)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToResponseDto).ToList();
    }

    public async Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string newStatus, string? note = null, string? changedBy = null)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return (false, "Không tìm thấy đơn hàng.");
                }

                var oldStatus = order.OrderStatus;

                // 1. Kiểm tra bảng chuyển trạng thái hợp lệ (State Machine):
                // "Đã hủy" là trạng thái kết thúc (Terminal state), không thể mở lại hay hủy tiếp
                if (oldStatus.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Đơn hàng này đã bị hủy trước đó. Không thể thay đổi trạng thái của đơn đã hủy.");
                }

                // "Hoàn thành" là đơn đã giao thành công, không thể hủy
                if (oldStatus.Equals("Hoàn thành", StringComparison.OrdinalIgnoreCase) && newStatus.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Đơn hàng đã hoàn thành và giao hàng thành công, không thể hủy đơn.");
                }

                if (oldStatus.Equals(newStatus, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, $"Đơn hàng hiện tại đã ở trạng thái '{newStatus}'.");
                }

                order.OrderStatus = newStatus;
                order.UpdatedAt = DateTime.UtcNow;

                // 2. Đồng bộ trạng thái thanh toán và bảng Payments
                if (newStatus.Equals("Hoàn thành", StringComparison.OrdinalIgnoreCase) ||
                    newStatus.Equals("Đã thanh toán", StringComparison.OrdinalIgnoreCase))
                {
                    order.PaymentStatus = "Đã thanh toán";
                    var latestPayment = order.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
                    if (latestPayment != null)
                    {
                        latestPayment.PaymentStatus = "Thành công";
                        latestPayment.PaymentDate = DateTime.UtcNow;
                        latestPayment.Note = $"Xác nhận thanh toán thành công bởi {changedBy ?? "Quản trị viên"}";
                    }
                    else
                    {
                        order.Payments.Add(new Payment
                        {
                            PaymentMethod = order.PaymentMethod,
                            Amount = order.TotalAmount,
                            PaymentStatus = "Thành công",
                            PaymentDate = DateTime.UtcNow,
                            Note = $"Thanh toán thành công xác nhận bởi {changedBy ?? "Quản trị viên"}"
                        });
                    }
                }
                else if (newStatus.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase))
                {
                    order.PaymentStatus = "Đã hủy";

                    // Hoàn lại số lượng tồn kho chính xác một lần duy nhất bằng UPDATE nguyên tử
                    foreach (var item in order.OrderItems)
                    {
                        if (_db.Database.IsRelational())
                        {
                            await _db.Products
                                .Where(p => p.ProductId == item.ProductId)
                                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + item.Quantity));
                        }
                        else
                        {
                            var p = await _db.Products.FindAsync(item.ProductId);
                            if (p != null)
                            {
                                p.StockQuantity += item.Quantity;
                                await _db.SaveChangesAsync();
                            }
                        }
                    }

                    var latestPayment = order.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
                    if (latestPayment != null)
                    {
                        latestPayment.PaymentStatus = "Đã hủy";
                        latestPayment.Note = $"Đơn hàng bị hủy: {note ?? "Hủy bởi quản trị viên"}";
                    }
                }

                order.StatusHistories.Add(new OrderStatusHistory
                {
                    Status = newStatus,
                    ChangedBy = changedBy ?? "Quản trị viên",
                    ChangedAt = DateTime.UtcNow,
                    Note = note ?? $"Cập nhật trạng thái từ '{oldStatus}' sang '{newStatus}'"
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return (true, $"Đã cập nhật trạng thái đơn #{order.OrderCode} thành công!");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, "Lỗi khi cập nhật trạng thái đơn hàng: " + ex.Message);
            }
        });
    }

    public async Task<AdminDashboardViewModel> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .ToListAsync();

        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .ToListAsync();

        // 1. Doanh thu tháng hiện tại (Chỉ tính các đơn không bị hủy và đã thanh toán / hoàn thành)
        var monthlyPaidOrders = orders
            .Where(o => o.OrderStatus != "Đã hủy" &&
                        (o.PaymentStatus == "Đã thanh toán" || o.OrderStatus == "Hoàn thành") &&
                        o.CreatedAt.Month == now.Month &&
                        o.CreatedAt.Year == now.Year)
            .ToList();

        decimal monthlyRevenue = monthlyPaidOrders.Sum(o => o.TotalAmount);

        // Doanh thu tháng trước để tính tốc độ tăng trưởng
        var lastMonth = now.AddMonths(-1);
        var lastMonthPaidOrders = orders
            .Where(o => o.OrderStatus != "Đã hủy" &&
                        (o.PaymentStatus == "Đã thanh toán" || o.OrderStatus == "Hoàn thành") &&
                        o.CreatedAt.Month == lastMonth.Month &&
                        o.CreatedAt.Year == lastMonth.Year)
            .ToList();
        decimal lastMonthRevenue = lastMonthPaidOrders.Sum(o => o.TotalAmount);
        double revenueGrowth = lastMonthRevenue > 0
            ? Math.Round((double)((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue * 100), 1)
            : (monthlyRevenue > 0 ? 100.0 : 0.0);

        // 2. Tổng số đơn hàng trong tháng
        var monthlyOrdersCount = orders
            .Count(o => o.CreatedAt.Month == now.Month && o.CreatedAt.Year == now.Year);

        // 3. Số laptop bán được thực tế trong tháng (loại bỏ đơn hủy)
        var monthlyLaptopsSold = orders
            .Where(o => o.OrderStatus != "Đã hủy" && o.CreatedAt.Month == now.Month && o.CreatedAt.Year == now.Year)
            .SelectMany(o => o.OrderItems)
            .Sum(i => i.Quantity);

        // 4. Số sản phẩm sắp hết hàng (<= 5 máy)
        int lowStockCount = products.Count(p => p.StockQuantity <= 5);

        // 5. Danh sách đơn hàng mới nhất
        var recentOrdersDto = orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(6)
            .Select(o => new RecentOrderDto
            {
                OrderCode = o.OrderCode,
                CustomerName = o.CustomerName,
                ProductName = o.OrderItems.FirstOrDefault()?.ProductName ?? "Laptop",
                OrderDate = o.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                TotalAmount = o.TotalAmount,
                Status = o.OrderStatus,
                StatusBadgeClass = GetBadgeClass(o.OrderStatus)
            })
            .ToList();

        // 6. Top sản phẩm bán chạy nhất tính từ chi tiết đơn hàng thực tế
        var nonCancelledOrderItems = orders
            .Where(o => o.OrderStatus != "Đã hủy")
            .SelectMany(o => o.OrderItems)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(x => x.Quantity) })
            .ToDictionary(x => x.ProductId, x => x.TotalSold);

        var topSelling = products
            .Select(p => new TopProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ThumbnailUrl = p.ThumbnailUrl,
                BrandName = p.Brand?.BrandName ?? "",
                Price = p.DiscountPrice ?? p.Price,
                SoldCount = nonCancelledOrderItems.TryGetValue(p.ProductId, out int sold) ? sold : 0
            })
            .OrderByDescending(p => p.SoldCount)
            .ThenBy(p => p.ProductId)
            .Take(5)
            .ToList();

        return new AdminDashboardViewModel
        {
            MonthlyRevenue = monthlyRevenue,
            RevenueGrowth = revenueGrowth,
            NewOrdersCount = monthlyOrdersCount,
            LaptopsSoldCount = monthlyLaptopsSold,
            SalesGrowth = revenueGrowth,
            LowStockCount = lowStockCount,
            RecentOrders = recentOrdersDto,
            TopSellingProducts = topSelling
        };
    }

    private static string GetBadgeClass(string status)
    {
        return status switch
        {
            "Đã thanh toán" or "Hoàn thành" => "badge-paid",
            "Đang xử lý" => "badge-processing",
            "Đã xác nhận" => "badge-confirmed",
            "Đã hủy" => "badge-out-of-stock",
            _ => "badge-in-stock"
        };
    }

    private static OrderResponseDto MapToResponseDto(Order o)
    {
        return new OrderResponseDto
        {
            OrderId = o.OrderId,
            OrderCode = o.OrderCode,
            CustomerName = o.CustomerName,
            PhoneNumber = o.PhoneNumber,
            ShippingAddress = o.ShippingAddress,
            TotalAmount = o.TotalAmount,
            PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus,
            OrderStatus = o.OrderStatus,
            CreatedAt = o.CreatedAt,
            Items = o.OrderItems.Select(i => new OrderItemResponseDto
            {
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                SelectedSpecs = i.SelectedSpecs,
                ThumbnailUrl = i.ThumbnailUrl,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }
}
