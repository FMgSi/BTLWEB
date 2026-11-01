using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly IInstallmentService _installmentService;

    public OrderService(AppDbContext db, IInstallmentService installmentService)
    {
        _db = db;
        _installmentService = installmentService;
    }

    public async Task<(bool Success, string Message, OrderResponseDto? Order)> CreateOrderAsync(CreateOrderRequest req, int? userId = null)
    {
        if (req.Items == null || !req.Items.Any())
        {
            return (false, "Giỏ hàng rỗng, không thể tạo đơn hàng.", null);
        }

        // Chống tạo đơn trùng lặp (Idempotent Request Handling)
        if (!string.IsNullOrWhiteSpace(req.IdempotencyKey))
        {
            var existingOrder = await _db.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .Include(o => o.InstallmentContract)
                    .ThenInclude(c => c!.Schedules)
                .FirstOrDefaultAsync(o => o.IdempotencyKey == req.IdempotencyKey.Trim());
            if (existingOrder != null)
            {
                return (true, "Đơn hàng đã được tiếp nhận thành công.", MapToResponseDto(existingOrder));
            }
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

                var variantIds = req.Items.Where(i => i.VariantId.HasValue).Select(i => i.VariantId!.Value).Distinct().ToList();
                var variants = new Dictionary<int, ProductVariant>();
                if (variantIds.Any())
                {
                    variants = await _db.ProductVariants
                        .Include(v => v.Color)
                        .Where(v => variantIds.Contains(v.VariantId))
                        .ToDictionaryAsync(v => v.VariantId);
                }

                // Tập hợp các sản phẩm đã có phiên bản đang hoạt động (dùng để chặn mua thiếu VariantId)
                var productsWithVariants = await _db.ProductVariants
                    .AsNoTracking()
                    .Where(v => productIds.Contains(v.ProductId) && v.IsActive)
                    .Select(v => v.ProductId)
                    .Distinct()
                    .ToListAsync();
                var productsWithVariantsSet = new HashSet<int>(productsWithVariants);

                decimal subTotal = 0;
                var orderItems = new List<OrderItem>();
                var inMemoryDeductions = new List<(ProductVariant? v, Product p, int qty)>();

                async Task RollbackAsync()
                {
                    await tx.RollbackAsync();
                    if (!_db.Database.IsRelational())
                    {
                        foreach (var d in inMemoryDeductions)
                        {
                            if (d.v != null) d.v.StockQuantity += d.qty;
                            d.p.StockQuantity += d.qty;
                        }
                        await _db.SaveChangesAsync();
                    }
                }

                foreach (var itemReq in req.Items)
                {
                    if (!products.TryGetValue(itemReq.ProductId, out var product))
                    {
                        await RollbackAsync();
                        return (false, $"Sản phẩm mã #{itemReq.ProductId} không tồn tại trên hệ thống.", null);
                    }

                    if (!product.IsActive)
                    {
                        await RollbackAsync();
                        return (false, $"Sản phẩm '{product.ProductName}' hiện đã ngừng kinh doanh.", null);
                    }

                    decimal unitPrice;
                    string specSnapshot = itemReq.SelectedSpecs ?? "";
                    string? colorName = itemReq.ColorName;
                    int? variantId = null;
                    string? itemThumbnail = product.ThumbnailUrl;

                    if (itemReq.VariantId.HasValue)
                    {
                        if (!variants.TryGetValue(itemReq.VariantId.Value, out var variant))
                        {
                            await RollbackAsync();
                            return (false, $"Phiên bản sản phẩm #{itemReq.VariantId.Value} không tồn tại trên hệ thống.", null);
                        }

                        if (variant.ProductId != itemReq.ProductId)
                        {
                            await RollbackAsync();
                            return (false, $"Phiên bản '{variant.SKU}' không thuộc sản phẩm '{product.ProductName}'.", null);
                        }

                        if (!variant.IsActive)
                        {
                            await RollbackAsync();
                            return (false, $"Phiên bản '{variant.SKU}' ({variant.Color?.Name}) hiện đã ngừng kinh doanh.", null);
                        }

                        // CHỐNG BÁN VƯỢT TỒN KHO TRÊN PHIÊN BẢN (MÀU SẮC + CẤU HÌNH CONCURRENCY SAFE):
                        int variantRowsAffected;
                        if (_db.Database.IsRelational())
                        {
                            variantRowsAffected = await _db.ProductVariants
                                .Where(v => v.VariantId == variant.VariantId && v.IsActive && v.StockQuantity >= itemReq.Quantity)
                                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity - itemReq.Quantity));
                        }
                        else
                        {
                            if (variant.StockQuantity >= itemReq.Quantity)
                            {
                                variant.StockQuantity -= itemReq.Quantity;
                                await _db.SaveChangesAsync();
                                variantRowsAffected = 1;
                            }
                            else
                            {
                                variantRowsAffected = 0;
                            }
                        }

                        if (variantRowsAffected == 0)
                        {
                            await RollbackAsync();
                            var freshV = await _db.ProductVariants.AsNoTracking().FirstOrDefaultAsync(v => v.VariantId == variant.VariantId);
                            int currentStock = freshV?.StockQuantity ?? 0;
                            string cName = variant.Color?.Name ?? "đã chọn";
                            return (false, $"Sản phẩm '{product.ProductName}' - Màu {cName} (RAM {variant.RamGB}GB/SSD {variant.StorageGB}GB) không đủ tồn kho (chỉ còn {currentStock} máy). Vui lòng điều chỉnh giỏ hàng!", null);
                        }

                        // Đồng thời trừ tổng tồn kho trên Product (kiểm tra chặt chẽ số dòng bị ảnh hưởng):
                        int productRowsAffected;
                        if (_db.Database.IsRelational())
                        {
                            productRowsAffected = await _db.Products
                                .Where(p => p.ProductId == itemReq.ProductId && p.StockQuantity >= itemReq.Quantity)
                                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - itemReq.Quantity));
                        }
                        else
                        {
                            if (product.StockQuantity >= itemReq.Quantity)
                            {
                                product.StockQuantity -= itemReq.Quantity;
                                await _db.SaveChangesAsync();
                                productRowsAffected = 1;
                                inMemoryDeductions.Add((variant, product, itemReq.Quantity));
                            }
                            else
                            {
                                productRowsAffected = 0;
                            }
                        }

                        if (productRowsAffected == 0)
                        {
                            // Nếu kho tổng Product không đủ (dữ liệu kho bị lệch), hoàn lại variant đã trừ và rollback
                            if (_db.Database.IsRelational())
                            {
                                await _db.ProductVariants
                                    .Where(v => v.VariantId == variant.VariantId)
                                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + itemReq.Quantity));
                            }
                            else
                            {
                                variant.StockQuantity += itemReq.Quantity;
                                await _db.SaveChangesAsync();
                            }

                            await RollbackAsync();
                            var freshP = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == itemReq.ProductId);
                            int currentProductStock = freshP?.StockQuantity ?? 0;
                            return (false, $"Sản phẩm '{product.ProductName}' không đủ tồn kho tổng (chỉ còn {currentProductStock} máy). Vui lòng điều chỉnh giỏ hàng!", null);
                        }

                        variantId = variant.VariantId;
                        colorName = variant.Color?.Name ?? itemReq.ColorName;
                        unitPrice = variant.EffectivePrice;
                        if (!string.IsNullOrWhiteSpace(variant.ImageUrl))
                        {
                            itemThumbnail = variant.ImageUrl;
                        }

                        if (string.IsNullOrWhiteSpace(specSnapshot))
                        {
                            specSnapshot = $"Màu {colorName} • RAM {variant.RamGB}GB • SSD {variant.StorageGB}GB";
                        }
                    }
                    else
                    {
                        // Kiểm tra nghiệp vụ: Mẫu laptop đã có phiên bản màu sắc thì BẮT BUỘC khách phải chọn phiên bản
                        if (productsWithVariantsSet.Contains(itemReq.ProductId))
                        {
                            await RollbackAsync();
                            return (false, $"Sản phẩm '{product.ProductName}' có nhiều phiên bản màu sắc và cấu hình. Vui lòng chọn phiên bản cụ thể trước khi đặt hàng!", null);
                        }

                        // FALLBACK: Đơn hàng theo sản phẩm gốc (chỉ dành cho sản phẩm cũ chưa chuyển đổi sang phiên bản)
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
                                inMemoryDeductions.Add((null, product, itemReq.Quantity));
                            }
                            else
                            {
                                rowsAffected = 0;
                            }
                        }

                        if (rowsAffected == 0)
                        {
                            await RollbackAsync();
                            var freshP = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == itemReq.ProductId);
                            int currentStock = freshP?.StockQuantity ?? 0;
                            return (false, $"Sản phẩm '{product.ProductName}' không đủ tồn kho (chỉ còn {currentStock} máy). Vui lòng điều chỉnh giỏ hàng!", null);
                        }

                        unitPrice = product.DiscountPrice ?? product.Price;
                        if (string.IsNullOrWhiteSpace(specSnapshot) && product.Specification != null)
                        {
                            specSnapshot = $"RAM {product.Specification.RamGB}GB • {product.Specification.StorageGB}GB {product.Specification.StorageType}";
                        }
                    }

                    decimal itemTotal = unitPrice * itemReq.Quantity;
                    subTotal += itemTotal;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.ProductId,
                        VariantId = variantId,
                        ColorName = colorName,
                        ProductName = product.ProductName,
                        SelectedSpecs = specSnapshot,
                        ThumbnailUrl = itemThumbnail,
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

                bool isInstallment = req.PurchaseMode?.Equals("Installment", StringComparison.OrdinalIgnoreCase) == true;
                int termMonths = (req.TermMonths == 3 || req.TermMonths == 6 || req.TermMonths == 9 || req.TermMonths == 12)
                    ? req.TermMonths.Value
                    : 6;

                InstallmentPlanDto? plan = null;
                if (isInstallment)
                {
                    plan = _installmentService.CalculatePlan(subTotal, termMonths);
                }

                string initialPaymentStatus = isInstallment
                    ? "Chờ trả trước"
                    : ((req.PaymentMethod.Contains("VietQR", StringComparison.OrdinalIgnoreCase) ||
                        req.PaymentMethod.Contains("Banking", StringComparison.OrdinalIgnoreCase))
                        ? "Chờ xác nhận"
                        : "Chờ thanh toán");

                string initialOrderStatus = "Đang xử lý";

                var order = new Order
                {
                    OrderCode = orderCode,
                    IdempotencyKey = req.IdempotencyKey?.Trim(),
                    UserId = userId,
                    CustomerName = req.CustomerName.Trim(),
                    PhoneNumber = req.PhoneNumber.Trim(),
                    Email = req.Email?.Trim(),
                    ShippingAddress = req.ShippingAddress.Trim(),
                    Notes = req.Notes?.Trim(),
                    SubTotal = subTotal,
                    DiscountAmount = 0,
                    TotalAmount = subTotal,
                    PurchaseMode = isInstallment ? "Installment" : "Full",
                    PaymentMethod = req.PaymentMethod,
                    PaymentStatus = initialPaymentStatus,
                    OrderStatus = initialOrderStatus,
                    CreatedAt = DateTime.UtcNow,
                    OrderItems = orderItems
                };

                if (isInstallment && plan != null)
                {
                    string prepayStatus = (req.PaymentMethod.Contains("VietQR", StringComparison.OrdinalIgnoreCase) ||
                                           req.PaymentMethod.Contains("Banking", StringComparison.OrdinalIgnoreCase))
                                           ? "Chờ xác nhận"
                                           : "Chờ thanh toán";

                    order.Payments.Add(new Payment
                    {
                        PaymentMethod = req.PaymentMethod,
                        Amount = plan.PrepayAmount,
                        PaymentStatus = prepayStatus,
                        PaymentType = "Prepay",
                        PaymentDate = DateTime.UtcNow,
                        Note = $"Khởi tạo thanh toán trả trước 30% ({plan.PrepayAmount:N0}đ) qua {req.PaymentMethod}"
                    });

                    // Tạo hợp đồng trả góp cùng trong transaction với đơn hàng và trừ kho
                    var contract = new InstallmentContract
                    {
                        ContractCode = $"HDTG{DateTime.Now:yyMMdd}-{orderCode.Substring(orderCode.Length - 4)}",
                        TermMonths = plan.TermMonths,
                        PrepayPercent = plan.PrepayPercent,
                        PrepayAmount = plan.PrepayAmount,
                        TotalFinancedAmount = plan.FinancedAmount,
                        InterestRate = 0.00m,
                        MonthlyInstallmentAmount = plan.MonthlyInstallmentAmount,
                        Status = "Chờ trả trước",
                        CreatedAt = DateTime.UtcNow,
                        Notes = req.Notes
                    };

                    foreach (var p in plan.Periods)
                    {
                        contract.Schedules.Add(new InstallmentSchedule
                        {
                            PeriodNumber = p.PeriodNumber,
                            DueDate = p.DueDate,
                            Amount = p.Amount,
                            PaidAmount = 0,
                            Status = "Chưa thanh toán"
                        });
                    }

                    order.InstallmentContract = contract;
                }
                else
                {
                    order.Payments.Add(new Payment
                    {
                        PaymentMethod = req.PaymentMethod,
                        Amount = subTotal,
                        PaymentStatus = initialPaymentStatus,
                        PaymentType = "Full",
                        PaymentDate = DateTime.UtcNow,
                        Note = $"Khởi tạo giao dịch {req.PaymentMethod}"
                    });
                }

                order.StatusHistories.Add(new OrderStatusHistory
                {
                    Status = initialOrderStatus,
                    ChangedBy = "Hệ thống",
                    ChangedAt = DateTime.UtcNow,
                    Note = isInstallment 
                        ? $"Khách hàng đặt mua trả góp {termMonths} tháng (trả trước 30%: {plan?.PrepayAmount:N0}đ)"
                        : "Khách hàng tạo đơn đặt hàng trực tuyến"
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
                .ThenInclude(i => i.Variant)
            .Include(o => o.Payments)
            .Include(o => o.InstallmentContract)
                .ThenInclude(c => c!.Schedules)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o =>
                o.OrderCode.ToLower().Contains(s) ||
                o.CustomerName.ToLower().Contains(s) ||
                o.PhoneNumber.Contains(s) ||
                (o.Email != null && o.Email.ToLower().Contains(s)) ||
                (o.InstallmentContract != null && o.InstallmentContract.ContractCode.ToLower().Contains(s))
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
                .ThenInclude(i => i.Variant)
            .Include(o => o.Payments)
            .Include(o => o.InstallmentContract)
                .ThenInclude(c => c!.Schedules)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode.Trim());

        return o == null ? null : MapToResponseDto(o);
    }

    public async Task<List<OrderResponseDto>> GetUserOrdersAsync(int userId)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Variant)
            .Include(o => o.Payments)
            .Include(o => o.InstallmentContract)
                .ThenInclude(c => c!.Schedules)
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
                    .Include(o => o.InstallmentContract)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return (false, "Không tìm thấy đơn hàng.");
                }

                var oldStatus = order.OrderStatus;

                if (oldStatus.Equals(newStatus, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, $"Đơn hàng hiện tại đã ở trạng thái '{newStatus}'.");
                }

                // 1. Kiểm tra máy trạng thái hữu hạn (Finite State Machine) - Chặn chuyển lùi hoặc trạng thái bất hợp lệ
                var allowedTransitions = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Chờ xác nhận", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Đang xử lý", "Đang giao", "Đang giao hàng", "Đã hủy" } },
                    { "Đang xử lý",   new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Đang giao", "Đang giao hàng", "Hoàn thành", "Đã hủy" } },
                    { "Đang giao",    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Hoàn thành", "Đã hủy" } },
                    { "Đang giao hàng", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Hoàn thành", "Đã hủy" } },
                    { "Hoàn thành",   new HashSet<string>(StringComparer.OrdinalIgnoreCase) { } }, // Terminal: Đã giao khách, không thể đổi
                    { "Đã hủy",       new HashSet<string>(StringComparer.OrdinalIgnoreCase) { } }  // Terminal: Đã hủy, không thể mở lại
                };

                if (allowedTransitions.TryGetValue(oldStatus, out var nextAllowed) && !nextAllowed.Contains(newStatus))
                {
                    return (false, $"Không thể chuyển trạng thái từ '{oldStatus}' sang '{newStatus}'. Luồng xử lý đơn hàng không cho phép thao tác này.");
                }

                // CHỐNG CONCURRENCY KHI HỦY ĐƠN:
                // Cập nhật trạng thái có điều kiện trực tiếp tại CSDL để ngăn chặn hai request hủy đồng thời cùng hoàn kho
                if (_db.Database.IsRelational())
                {
                    var updatedRows = await _db.Orders
                        .Where(o => o.OrderId == orderId && o.OrderStatus == oldStatus && o.OrderStatus != "Đã hủy")
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(o => o.OrderStatus, newStatus)
                            .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));

                    if (updatedRows == 0)
                    {
                        await tx.RollbackAsync();
                        return (false, "Trạng thái đơn hàng đã được cập nhật bởi một thao tác khác hoặc đơn đã bị hủy.");
                    }
                }

                order.OrderStatus = newStatus;
                order.UpdatedAt = DateTime.UtcNow;

                // 2. Đồng bộ trạng thái thanh toán và bảng Payments
                bool isInstallmentOrder = order.PurchaseMode.Equals("Installment", StringComparison.OrdinalIgnoreCase);

                if (newStatus.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase))
                {
                    order.PaymentStatus = "Đã hủy";
                    if (order.InstallmentContract != null)
                    {
                        order.InstallmentContract.Status = "Đã hủy";
                    }

                    // Hoàn lại số lượng tồn kho chính xác một lần duy nhất bằng UPDATE nguyên tử
                    // Hoàn trả đúng phiên bản (Màu sắc + Cấu hình) nếu đơn có liên kết VariantId, đồng thời cập nhật Product
                    foreach (var item in order.OrderItems)
                    {
                        if (item.VariantId.HasValue)
                        {
                            if (_db.Database.IsRelational())
                            {
                                await _db.ProductVariants
                                    .Where(v => v.VariantId == item.VariantId.Value)
                                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + item.Quantity));

                                await _db.Products
                                    .Where(p => p.ProductId == item.ProductId)
                                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + item.Quantity));
                            }
                            else
                            {
                                var v = await _db.ProductVariants.FindAsync(item.VariantId.Value);
                                if (v != null)
                                {
                                    v.StockQuantity += item.Quantity;
                                }
                                var p = await _db.Products.FindAsync(item.ProductId);
                                if (p != null)
                                {
                                    p.StockQuantity += item.Quantity;
                                }
                                await _db.SaveChangesAsync();
                            }
                        }
                        else
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
                    }

                    var latestPayment = order.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
                    if (latestPayment != null && latestPayment.PaymentStatus != "Thành công")
                    {
                        latestPayment.PaymentStatus = "Đã hủy";
                        latestPayment.Note = $"Đơn hàng bị hủy: {note ?? "Hủy bởi quản trị viên"}";
                    }

                    // Cảnh báo hoàn tiền nếu đơn trả góp hoặc đơn trả thẳng đã thực thu tiền
                    var totalCollected = order.Payments.Where(p => p.PaymentStatus == "Thành công").Sum(p => p.Amount);
                    if (totalCollected > 0)
                    {
                        note = (string.IsNullOrWhiteSpace(note) ? "" : note + " | ") + 
                               $"Cảnh báo kế toán: Đơn đã hủy nhưng đã thực thu {totalCollected:N0}đ, cần hoàn tiền lại cho khách hàng.";
                    }
                }
                else if (isInstallmentOrder)
                {
                    // Đối với đơn hàng trả góp: Giao máy xong (Đang giao / Hoàn thành) vẫn có thể còn nợ 70%.
                    // Giao hàng KHÔNG làm thay đổi dư nợ và KHÔNG tự động đánh dấu đã thanh toán toàn bộ.
                    // Chỉ cập nhật trạng thái thanh toán từ số tiền thực thu / tiến độ hợp đồng.
                    if (order.InstallmentContract != null)
                    {
                        if (order.InstallmentContract.Status == "Đã tất toán")
                        {
                            order.PaymentStatus = "Đã tất toán";
                        }
                        else if (order.InstallmentContract.Status == "Đang trả góp")
                        {
                            order.PaymentStatus = "Đang trả góp";
                        }
                        else
                        {
                            order.PaymentStatus = "Chờ trả trước";
                        }
                    }
                }
                else if (newStatus.Equals("Hoàn thành", StringComparison.OrdinalIgnoreCase) ||
                         newStatus.Equals("Đã thanh toán", StringComparison.OrdinalIgnoreCase))
                {
                    // Đơn trả thẳng thông thường (PurchaseMode == "Full")
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
                            PaymentType = "Full",
                            PaymentDate = DateTime.UtcNow,
                            Note = $"Thanh toán thành công xác nhận bởi {changedBy ?? "Quản trị viên"}"
                        });
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

        var (waitingPrepay, activeContracts, totalDebt) = await _installmentService.GetInstallmentDashboardMetricsAsync();

        return new AdminDashboardViewModel
        {
            MonthlyRevenue = monthlyRevenue,
            RevenueGrowth = revenueGrowth,
            NewOrdersCount = monthlyOrdersCount,
            LaptopsSoldCount = monthlyLaptopsSold,
            SalesGrowth = revenueGrowth,
            LowStockCount = lowStockCount,
            WaitingInstallmentPrepayCount = waitingPrepay,
            ActiveInstallmentContractsCount = activeContracts,
            TotalInstallmentDebt = totalDebt,
            RecentOrders = recentOrdersDto,
            TopSellingProducts = topSelling
        };
    }

    private static string GetBadgeClass(string status)
    {
        return status switch
        {
            "Đã thanh toán" or "Hoàn thành" or "Đã tất toán" => "badge-paid",
            "Đang xử lý" or "Đang trả góp" => "badge-processing",
            "Đã xác nhận" => "badge-confirmed",
            "Đã hủy" => "badge-out-of-stock",
            _ => "badge-in-stock"
        };
    }

    private static OrderResponseDto MapToResponseDto(Order o)
    {
        decimal? prepay = o.InstallmentContract?.PrepayAmount;
        decimal? remainingDebt = null;
        if (o.PurchaseMode.Equals("Installment", StringComparison.OrdinalIgnoreCase))
        {
            var successfulPayments = o.Payments != null ? o.Payments.Where(p => p.PaymentStatus == "Thành công").ToList() : new List<Payment>();
            decimal totalPaid = successfulPayments.Sum(p => p.Amount);
            remainingDebt = Math.Max(0, o.TotalAmount - totalPaid);
        }

        InstallmentContractDetailDto? contractDto = null;
        if (o.InstallmentContract != null)
        {
            var c = o.InstallmentContract;
            var successfulPayments = o.Payments != null ? o.Payments.Where(p => p.PaymentStatus == "Thành công").ToList() : new List<Payment>();
            decimal totalPaid = successfulPayments.Sum(p => p.Amount);
            decimal debt = Math.Max(0, o.TotalAmount - totalPaid);
            bool isPrepayPaid = c.Status != "Chờ trả trước" || successfulPayments.Any(p => p.PaymentType == "Prepay");

            var schedulesDto = c.Schedules
                .OrderBy(s => s.PeriodNumber)
                .Select(s => new InstallmentScheduleDto
                {
                    ScheduleId = s.ScheduleId,
                    PeriodNumber = s.PeriodNumber,
                    DueDate = s.DueDate,
                    Amount = s.Amount,
                    PaidAmount = s.PaidAmount,
                    Status = (s.Status != "Đã thanh toán" && s.DueDate < DateTime.UtcNow) ? "Quá hạn" : s.Status,
                    PaidAt = s.PaidAt,
                    CollectedBy = s.CollectedBy,
                    PaymentId = s.PaymentId
                }).ToList();

            var nextPeriod = schedulesDto.FirstOrDefault(s => s.Status != "Đã thanh toán");
            if (nextPeriod != null) nextPeriod.IsCurrentDue = true;

            contractDto = new InstallmentContractDetailDto
            {
                ContractId = c.ContractId,
                ContractCode = c.ContractCode,
                OrderId = c.OrderId,
                OrderCode = o.OrderCode,
                CustomerName = o.CustomerName,
                PhoneNumber = o.PhoneNumber,
                ShippingAddress = o.ShippingAddress,
                TotalOrderAmount = o.TotalAmount,
                TermMonths = c.TermMonths,
                PrepayPercent = c.PrepayPercent,
                PrepayAmount = c.PrepayAmount,
                PrepayStatus = isPrepayPaid ? "Đã thanh toán" : "Chưa thanh toán",
                TotalFinancedAmount = c.TotalFinancedAmount,
                InterestRate = c.InterestRate,
                MonthlyInstallmentAmount = c.MonthlyInstallmentAmount,
                Status = c.Status,
                OrderStatus = o.OrderStatus,
                CreatedAt = c.CreatedAt,
                ActivatedAt = c.ActivatedAt,
                CompletedAt = c.CompletedAt,
                Notes = c.Notes,
                TotalPaid = totalPaid,
                RemainingDebt = debt,
                PaidPeriodsCount = schedulesDto.Count(s => s.Status == "Đã thanh toán"),
                OverduePeriodsCount = schedulesDto.Count(s => s.Status == "Quá hạn"),
                NextPeriodToPay = nextPeriod,
                Schedules = schedulesDto,
                Payments = (o.Payments ?? new List<Payment>())
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new InstallmentPaymentReceiptDto
                    {
                        PaymentId = p.PaymentId,
                        PaymentType = p.PaymentType,
                        Amount = p.Amount,
                        PaymentMethod = p.PaymentMethod,
                        PaymentStatus = p.PaymentStatus,
                        PaymentDate = p.PaymentDate,
                        ConfirmedBy = p.ConfirmedBy,
                        TransactionReference = p.TransactionReference,
                        Note = p.Note
                    }).ToList()
            };
        }

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
            PurchaseMode = o.PurchaseMode,
            PrepayAmount = prepay,
            RemainingDebt = remainingDebt,
            InstallmentContract = contractDto,
            CreatedAt = o.CreatedAt,
            Items = o.OrderItems.Select(i => new OrderItemResponseDto
            {
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                VariantId = i.VariantId,
                ColorName = i.ColorName,
                SKU = i.Variant?.SKU,
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
