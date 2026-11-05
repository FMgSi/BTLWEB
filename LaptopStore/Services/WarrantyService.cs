using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Services;

public class WarrantyService : IWarrantyService
{
    private readonly AppDbContext _db;

    public WarrantyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ServiceCatalogDto>> GetCatalogAsync(string? type = null)
    {
        var query = _db.ServiceCatalog.AsNoTracking().Where(s => s.IsActive);
        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(s => s.ServiceType.ToLower() == type.ToLower());
        }

        return await query.Select(s => new ServiceCatalogDto
        {
            ServiceId = s.ServiceId,
            ServiceCode = s.ServiceCode,
            ServiceName = s.ServiceName,
            ServiceType = s.ServiceType,
            ApplicableConditions = s.ApplicableConditions,
            EstimatedCost = s.EstimatedCost,
            EstimatedTime = s.EstimatedTime,
            ServiceWarrantyMonths = s.ServiceWarrantyMonths,
            IsActive = s.IsActive
        }).ToListAsync();
    }

    public async Task<List<SoldDeviceDto>> GetCustomerDevicesAsync(string? phoneNumber = null, int? userId = null, string? orderCode = null)
    {
        var query = _db.SoldDevices
            .AsNoTracking()
            .Include(d => d.OrderItem)
                .ThenInclude(i => i!.Order)
            .Include(d => d.OrderItem)
                .ThenInclude(i => i!.Product)
            .Include(d => d.OrderItem)
                .ThenInclude(i => i!.Variant)
            .AsQueryable();

        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(d => d.OrderItem != null && d.OrderItem.Order != null && d.OrderItem.Order.UserId == userId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(orderCode))
        {
            var cleanCode = orderCode.Trim();
            query = query.Where(d => d.OrderItem != null && d.OrderItem.Order != null &&
                (d.OrderItem.Order.OrderCode.Contains(cleanCode) || d.SerialNumber.Contains(cleanCode)));
        }
        else if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var cleanPhone = phoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
            query = query.Where(d => d.OrderItem != null && d.OrderItem.Order != null &&
                (d.OrderItem.Order.PhoneNumber.Replace(" ", "").Replace(".", "").Replace("-", "") == cleanPhone ||
                 d.OrderItem.Order.OrderCode.Contains(cleanPhone) ||
                 d.SerialNumber.Contains(cleanPhone)));
        }

        var devices = await query.OrderByDescending(d => d.DeliveredAt).ToListAsync();

        return devices.Select(d => new SoldDeviceDto
        {
            DeviceId = d.DeviceId,
            OrderItemId = d.OrderItemId,
            OrderId = d.OrderItem?.OrderId ?? 0,
            OrderCode = d.OrderItem?.Order?.OrderCode ?? "",
            CustomerName = d.OrderItem?.Order?.CustomerName ?? "",
            PhoneNumber = d.OrderItem?.Order?.PhoneNumber ?? "",
            SerialNumber = d.SerialNumber,
            ProductName = d.OrderItem?.ProductName ?? d.OrderItem?.Product?.ProductName ?? "Laptop",
            ColorName = d.OrderItem?.ColorName,
            SKU = d.OrderItem?.Variant?.SKU,
            ThumbnailUrl = d.OrderItem?.ThumbnailUrl ?? d.OrderItem?.Product?.ThumbnailUrl,
            DeliveredAt = d.DeliveredAt,
            WarrantyStartDate = d.WarrantyStartDate,
            WarrantyEndDate = d.WarrantyEndDate,
            WarrantyMonths = d.WarrantyMonths,
            IsUnderWarranty = DateTime.UtcNow <= d.WarrantyEndDate,
            Notes = d.Notes
        }).ToList();
    }

    public async Task<List<ServiceTicketDetailDto>> GetCustomerTicketsAsync(string? phoneNumber = null, int? userId = null)
    {
        var query = _db.ServiceTickets
            .AsNoTracking()
            .Include(x => x.Service)
            .Include(x => x.SoldDevice)
                .ThenInclude(d => d!.OrderItem)
            .Include(x => x.Quotes)
            .Include(x => x.Histories)
            .AsQueryable();

        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(t => t.UserId == userId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var cleanPhone = phoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
            query = query.Where(t => t.PhoneNumber.Replace(" ", "").Replace(".", "").Replace("-", "") == cleanPhone);
        }
        else
        {
            return new List<ServiceTicketDetailDto>();
        }

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tickets.Select(MapEntityToDetailDto).ToList();
    }

    public async Task<SoldDeviceDto?> GetDeviceBySerialAsync(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber)) return null;

        var clean = serialNumber.Trim();
        var d = await _db.SoldDevices
            .AsNoTracking()
            .Include(x => x.OrderItem)
                .ThenInclude(i => i!.Order)
            .Include(x => x.OrderItem)
                .ThenInclude(i => i!.Product)
            .FirstOrDefaultAsync(x => x.SerialNumber == clean);

        if (d == null) return null;

        return new SoldDeviceDto
        {
            DeviceId = d.DeviceId,
            OrderItemId = d.OrderItemId,
            OrderId = d.OrderItem?.OrderId ?? 0,
            OrderCode = d.OrderItem?.Order?.OrderCode ?? "",
            CustomerName = d.OrderItem?.Order?.CustomerName ?? "",
            PhoneNumber = d.OrderItem?.Order?.PhoneNumber ?? "",
            SerialNumber = d.SerialNumber,
            ProductName = d.OrderItem?.ProductName ?? d.OrderItem?.Product?.ProductName ?? "Laptop",
            ColorName = d.OrderItem?.ColorName,
            SKU = d.OrderItem?.Variant?.SKU,
            ThumbnailUrl = d.OrderItem?.ThumbnailUrl ?? d.OrderItem?.Product?.ThumbnailUrl,
            DeliveredAt = d.DeliveredAt,
            WarrantyStartDate = d.WarrantyStartDate,
            WarrantyEndDate = d.WarrantyEndDate,
            WarrantyMonths = d.WarrantyMonths,
            IsUnderWarranty = DateTime.UtcNow <= d.WarrantyEndDate,
            Notes = d.Notes
        };
    }

    public async Task<(bool Success, string Message, ServiceTicketDetailDto? Ticket)> CreateWarrantyTicketAsync(
        CreateWarrantyTicketRequest request, int? userId = null)
    {
        var device = await _db.SoldDevices
            .Include(d => d.OrderItem)
                .ThenInclude(i => i!.Order)
            .FirstOrDefaultAsync(d => d.DeviceId == request.SoldDeviceId);

        if (device == null)
        {
            return (false, "Không tìm thấy thiết bị tương ứng trong hệ thống máy đã bán.", null);
        }

        // Kiểm tra quyền sở hữu thiết bị
        var cleanPhone = request.PhoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        var orderPhone = (device.OrderItem?.Order?.PhoneNumber ?? "").Trim().Replace(" ", "").Replace(".", "").Replace("-", "");

        bool isOwner = false;
        if (userId.HasValue && userId.Value > 0 && device.OrderItem?.Order?.UserId == userId.Value)
        {
            isOwner = true;
        }
        else if (orderPhone == cleanPhone)
        {
            isOwner = true;
        }

        if (!isOwner)
        {
            return (false, "Số điện thoại không khớp với thông tin người mua trên hóa đơn của chiếc máy này.", null);
        }

        // Kiểm tra thời hạn bảo hành
        if (DateTime.UtcNow > device.WarrantyEndDate)
        {
            return (false, $"Thiết bị (Serial: {device.SerialNumber}) đã hết hạn bảo hành vào ngày {device.WarrantyEndDate:dd/MM/yyyy}. Quý khách vui lòng chuyển sang gửi yêu cầu 'Sửa chữa'.", null);
        }

        // Chống tiếp nhận trùng: Một máy đang nằm tại cửa hàng hoặc đang xử lý không được tạo phiếu mới
        var activeStatuses = new List<string> { "Chờ tiếp nhận", "Đã tiếp nhận", "Đang kiểm tra", "Chờ duyệt báo giá", "Đang xử lý", "Chờ trả máy" };
        var hasActiveTicket = await _db.ServiceTickets.AnyAsync(t => t.SoldDeviceId == device.DeviceId && activeStatuses.Contains(t.Status));
        if (hasActiveTicket)
        {
            return (false, $"Thiết bị (Serial: {device.SerialNumber}) hiện đang có một phiếu dịch vụ đang được tiếp nhận/xử lý tại cửa hàng.", null);
        }

        // Sinh mã phiếu duy nhất (BH-YYYYMMDD-XXXX)
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _db.ServiceTickets.CountAsync(t => t.TicketCode.StartsWith($"BH-{datePrefix}"));
        var ticketCode = $"BH-{datePrefix}-{(countToday + 1):D4}";

        var ticket = new ServiceTicket
        {
            TicketCode = ticketCode,
            TicketType = "Bảo hành",
            ServiceId = request.ServiceId,
            SoldDeviceId = device.DeviceId,
            UserId = userId ?? device.OrderItem?.Order?.UserId,
            CustomerName = request.CustomerName.Trim(),
            PhoneNumber = cleanPhone,
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            FaultDescription = request.FaultDescription.Trim(),
            Accessories = request.Accessories?.Trim(),
            Status = "Chờ tiếp nhận",
            FinalCost = 0, // Bảo hành ban đầu = 0đ
            CreatedAt = DateTime.UtcNow
        };

        ticket.Histories.Add(new ServiceTicketHistory
        {
            PreviousStatus = null,
            NewStatus = "Chờ tiếp nhận",
            ActionBy = request.CustomerName.Trim(),
            ActionAt = DateTime.UtcNow,
            Notes = "Khách hàng gửi yêu cầu bảo hành máy đã mua trực tuyến qua website."
        });

        _db.ServiceTickets.Add(ticket);
        await _db.SaveChangesAsync();

        var detail = await MapToDetailDtoAsync(ticket.TicketId);
        return (true, "Đã gửi yêu cầu bảo hành thành công!", detail);
    }

    public async Task<(bool Success, string Message, ServiceTicketDetailDto? Ticket)> CreateRepairTicketAsync(
        CreateRepairTicketRequest request, int? userId = null)
    {
        var cleanPhone = request.PhoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        SoldDevice? device = null;

        if (request.DeviceSource == "Purchased" && request.SoldDeviceId.HasValue)
        {
            device = await _db.SoldDevices
                .Include(d => d.OrderItem)
                    .ThenInclude(i => i!.Order)
                .FirstOrDefaultAsync(d => d.DeviceId == request.SoldDeviceId.Value);

            if (device != null)
            {
                var activeStatuses = new List<string> { "Chờ tiếp nhận", "Đã tiếp nhận", "Đang kiểm tra", "Chờ duyệt báo giá", "Đang xử lý", "Chờ trả máy" };
                var hasActive = await _db.ServiceTickets.AnyAsync(t => t.SoldDeviceId == device.DeviceId && activeStatuses.Contains(t.Status));
                if (hasActive)
                {
                    return (false, $"Thiết bị (Serial: {device.SerialNumber}) đang có phiếu dịch vụ đang được xử lý.", null);
                }
            }
        }

        // Sinh mã phiếu duy nhất (SC-YYYYMMDD-XXXX)
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _db.ServiceTickets.CountAsync(t => t.TicketCode.StartsWith($"SC-{datePrefix}"));
        var ticketCode = $"SC-{datePrefix}-{(countToday + 1):D4}";

        var ticket = new ServiceTicket
        {
            TicketCode = ticketCode,
            TicketType = "Sửa chữa",
            ServiceId = request.ServiceId,
            SoldDeviceId = device?.DeviceId,
            UserId = userId,
            CustomerName = request.CustomerName.Trim(),
            PhoneNumber = cleanPhone,
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            ExternalDeviceModel = request.DeviceSource == "External" ? request.ExternalDeviceModel?.Trim() : null,
            ExternalSerialNumber = request.DeviceSource == "External" ? request.ExternalSerialNumber?.Trim() : null,
            FaultDescription = request.FaultDescription.Trim(),
            Accessories = request.Accessories?.Trim(),
            Status = "Chờ tiếp nhận",
            FinalCost = 0, // Sẽ được cập nhật sau khi kỹ thuật kiểm tra và báo giá
            CreatedAt = DateTime.UtcNow
        };

        ticket.Histories.Add(new ServiceTicketHistory
        {
            PreviousStatus = null,
            NewStatus = "Chờ tiếp nhận",
            ActionBy = request.CustomerName.Trim(),
            ActionAt = DateTime.UtcNow,
            Notes = request.DeviceSource == "Purchased"
                ? "Khách hàng gửi yêu cầu sửa chữa cho máy đã mua."
                : $"Khách hàng gửi yêu cầu sửa chữa máy ngoài ({request.ExternalDeviceModel ?? "Laptop"})."
        });

        _db.ServiceTickets.Add(ticket);
        await _db.SaveChangesAsync();

        var detail = await MapToDetailDtoAsync(ticket.TicketId);
        return (true, "Đã gửi yêu cầu sửa chữa thành công!", detail);
    }

    public async Task<ServiceTicketDetailDto?> GetTicketByCodeAsync(string ticketCode, string? phoneVerification = null)
    {
        if (string.IsNullOrWhiteSpace(ticketCode)) return null;

        var cleanCode = ticketCode.Trim();
        var ticket = await _db.ServiceTickets.FirstOrDefaultAsync(t => t.TicketCode == cleanCode);
        if (ticket == null) return null;

        // Xác thực số điện thoại để bảo vệ quyền riêng tư nếu người dùng tra cứu ngoài
        if (!string.IsNullOrEmpty(phoneVerification))
        {
            var cleanPhone = phoneVerification.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
            if (ticket.PhoneNumber != cleanPhone)
            {
                return null;
            }
        }

        return await MapToDetailDtoAsync(ticket.TicketId);
    }

    public async Task<ServiceTicketDetailDto?> GetTicketByIdAsync(int ticketId)
    {
        return await MapToDetailDtoAsync(ticketId);
    }

    public async Task<(bool Success, string Message)> SubmitQuoteDecisionAsync(
        int ticketId, int quoteId, string decision, string? notes = null, string? customerPhone = null)
    {
        ServiceTicket? ticket = null;
        if (ticketId > 0)
        {
            ticket = await _db.ServiceTickets
                .Include(t => t.Quotes)
                .Include(t => t.Histories)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);
        }
        else if (quoteId > 0)
        {
            var quoteFound = await _db.ServiceQuotes.FirstOrDefaultAsync(q => q.QuoteId == quoteId);
            if (quoteFound != null)
            {
                ticket = await _db.ServiceTickets
                    .Include(t => t.Quotes)
                    .Include(t => t.Histories)
                    .FirstOrDefaultAsync(t => t.TicketId == quoteFound.TicketId);
            }
        }

        if (ticket == null) return (false, "Không tìm thấy phiếu dịch vụ tương ứng.");

        if (!string.IsNullOrEmpty(customerPhone))
        {
            var cleanPhone = customerPhone.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
            if (ticket.PhoneNumber != cleanPhone)
            {
                return (false, "Số điện thoại xác thực không khớp với người tạo phiếu.");
            }
        }

        var quote = ticket.Quotes.FirstOrDefault(q => q.QuoteId == quoteId);
        if (quote == null) return (false, "Không tìm thấy báo giá chỉ định.");

        if (quote.CustomerDecision != "Chờ duyệt")
        {
            return (false, $"Báo giá này đã được ghi nhận quyết định '{quote.CustomerDecision}' trước đó.");
        }

        if (decision == "Đồng ý")
        {
            quote.CustomerDecision = "Đồng ý";
            quote.DecisionAt = DateTime.UtcNow;
            quote.Notes = notes;

            var prev = ticket.Status;
            ticket.Status = "Đang xử lý";
            ticket.FinalCost = quote.Amount;

            ticket.Histories.Add(new ServiceTicketHistory
            {
                TicketId = ticket.TicketId,
                PreviousStatus = prev,
                NewStatus = "Đang xử lý",
                ActionBy = ticket.CustomerName,
                ActionAt = DateTime.UtcNow,
                Notes = $"Khách hàng đã ĐỒNG Ý báo giá chi phí: {quote.Amount:N0}đ ({quote.Description})"
            });
        }
        else if (decision == "Từ chối")
        {
            quote.CustomerDecision = "Từ chối";
            quote.DecisionAt = DateTime.UtcNow;
            quote.Notes = notes;

            var prev = ticket.Status;
            ticket.Status = "Chờ trả máy";

            ticket.Histories.Add(new ServiceTicketHistory
            {
                TicketId = ticket.TicketId,
                PreviousStatus = prev,
                NewStatus = "Chờ trả máy",
                ActionBy = ticket.CustomerName,
                ActionAt = DateTime.UtcNow,
                Notes = $"Khách hàng TỪ CHỐI báo giá ({quote.Amount:N0}đ). Cửa hàng hoàn tất thủ tục và chuyển sang chờ trả máy cho khách."
            });
        }
        else
        {
            return (false, "Quyết định không hợp lệ.");
        }

        await _db.SaveChangesAsync();
        return (true, "Đã cập nhật quyết định thành công.");
    }

    public async Task<List<ServiceTicketDetailDto>> GetAdminTicketsAsync(string? status = null, string? search = null, string? type = null)
    {
        var query = _db.ServiceTickets
            .AsNoTracking()
            .Include(t => t.Service)
            .Include(t => t.SoldDevice)
                .ThenInclude(d => d!.OrderItem)
            .Include(t => t.Quotes)
            .Include(t => t.Histories)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrEmpty(type) && type != "all")
        {
            query = query.Where(t => t.TicketType == type);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.Trim();
            query = query.Where(t =>
                t.TicketCode.Contains(s) ||
                t.CustomerName.Contains(s) ||
                t.PhoneNumber.Contains(s) ||
                (t.SoldDevice != null && t.SoldDevice.SerialNumber.Contains(s)) ||
                (t.ExternalSerialNumber != null && t.ExternalSerialNumber.Contains(s)));
        }

        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return tickets.Select(MapEntityToDetailDto).ToList();
    }

    public async Task<(bool Success, string Message)> UpdateTicketStatusByAdminAsync(
        int ticketId,
        string newStatus,
        string staffName,
        string? notes = null,
        string? physicalCondition = null,
        string? accessories = null,
        string? rejectReason = null,
        decimal? finalCost = null,
        int? warrantyMonthsAfterRepair = null)
    {
        var ticket = await _db.ServiceTickets
            .Include(t => t.Histories)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null) return (false, "Không tìm thấy phiếu dịch vụ.");

        var prevStatus = ticket.Status;
        ticket.Status = newStatus;
        ticket.AssignedStaff = staffName;

        if (!string.IsNullOrEmpty(physicalCondition)) ticket.PhysicalCondition = physicalCondition;
        if (!string.IsNullOrEmpty(accessories)) ticket.Accessories = accessories;
        if (!string.IsNullOrEmpty(rejectReason)) ticket.RejectReason = rejectReason;
        if (finalCost.HasValue && finalCost.Value >= 0) ticket.FinalCost = finalCost.Value;

        if (newStatus == "Đã tiếp nhận" && ticket.ReceivedAt == null)
        {
            ticket.ReceivedAt = DateTime.UtcNow;
        }
        else if (newStatus == "Chờ trả máy" && ticket.CompletedAt == null)
        {
            ticket.CompletedAt = DateTime.UtcNow;
            if (warrantyMonthsAfterRepair.HasValue && warrantyMonthsAfterRepair.Value > 0)
            {
                ticket.ServiceWarrantyUntil = DateTime.UtcNow.AddMonths(warrantyMonthsAfterRepair.Value);
            }
        }
        else if (newStatus == "Đã trả máy" && ticket.ReturnedAt == null)
        {
            ticket.ReturnedAt = DateTime.UtcNow;
        }

        ticket.Histories.Add(new ServiceTicketHistory
        {
            TicketId = ticket.TicketId,
            PreviousStatus = prevStatus,
            NewStatus = newStatus,
            ActionBy = staffName,
            ActionAt = DateTime.UtcNow,
            Notes = notes ?? $"Nhân viên {staffName} chuyển trạng thái sang: {newStatus}"
        });

        await _db.SaveChangesAsync();
        return (true, "Đã cập nhật trạng thái phiếu thành công.");
    }

    public async Task<(bool Success, string Message)> CreateQuoteAsync(
        int ticketId, string description, decimal amount, string staffName)
    {
        var ticket = await _db.ServiceTickets
            .Include(t => t.Quotes)
            .Include(t => t.Histories)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null) return (false, "Không tìm thấy phiếu dịch vụ.");

        if (amount < 0) return (false, "Số tiền báo giá phải >= 0đ.");

        var quote = new ServiceQuote
        {
            TicketId = ticket.TicketId,
            Description = description.Trim(),
            Amount = amount,
            CustomerDecision = "Chờ duyệt",
            CreatedAt = DateTime.UtcNow
        };

        var prev = ticket.Status;
        ticket.Status = "Chờ duyệt báo giá";
        ticket.Quotes.Add(quote);

        ticket.Histories.Add(new ServiceTicketHistory
        {
            TicketId = ticket.TicketId,
            PreviousStatus = prev,
            NewStatus = "Chờ duyệt báo giá",
            ActionBy = staffName,
            ActionAt = DateTime.UtcNow,
            Notes = $"Nhân viên {staffName} tạo báo giá: {amount:N0}đ ({description}). Chờ khách hàng duyệt."
        });

        await _db.SaveChangesAsync();
        return (true, "Đã tạo và gửi báo giá cho khách hàng thành công.");
    }

    public async Task<(bool Success, string Message)> AssignSerialsToOrderAsync(
        int orderId, List<OrderItemSerialsInput> items, string staffName)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null) return (false, "Không tìm thấy đơn hàng.");

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var itemInput in items)
            {
                var orderItem = order.OrderItems.FirstOrDefault(i => i.OrderItemId == itemInput.OrderItemId);
                if (orderItem == null) continue;

                var cleanedSerials = itemInput.SerialNumbers
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (cleanedSerials.Count != orderItem.Quantity)
                {
                    return (false, $"Sản phẩm '{orderItem.ProductName}' yêu cầu đúng {orderItem.Quantity} số Serial khác nhau (đã nhập: {cleanedSerials.Count}).");
                }

                // Kiểm tra trùng Serial với máy khác đã lưu
                var existingOtherSerials = await _db.SoldDevices
                    .Where(d => d.OrderItemId != orderItem.OrderItemId && cleanedSerials.Contains(d.SerialNumber))
                    .Select(d => d.SerialNumber)
                    .ToListAsync();

                if (existingOtherSerials.Any())
                {
                    return (false, $"Số Serial [{string.Join(", ", existingOtherSerials)}] đã được cấp cho một máy khác trong hệ thống!");
                }

                // Xóa các serial cũ của item này nếu cập nhật lại
                var oldDevices = await _db.SoldDevices.Where(d => d.OrderItemId == orderItem.OrderItemId).ToListAsync();
                if (oldDevices.Any())
                {
                    _db.SoldDevices.RemoveRange(oldDevices);
                }

                // Thêm các máy mới theo serial
                foreach (var sn in cleanedSerials)
                {
                    _db.SoldDevices.Add(new SoldDevice
                    {
                        OrderItemId = orderItem.OrderItemId,
                        SerialNumber = sn,
                        DeliveredAt = order.CreatedAt,
                        WarrantyStartDate = order.CreatedAt,
                        WarrantyEndDate = order.CreatedAt.AddMonths(12),
                        WarrantyMonths = 12,
                        Notes = $"Gán bởi {staffName} lúc {DateTime.UtcNow:dd/MM/yyyy HH:mm} cho đơn #{order.OrderCode}"
                    });
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, "Đã cập nhật danh sách Serial thành công.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, "Lỗi khi gán số Serial: " + ex.Message);
        }
    }

    public async Task<List<SoldDeviceDto>> GetAllSoldDevicesAsync(string? search = null)
    {
        var query = _db.SoldDevices
            .AsNoTracking()
            .Include(d => d.OrderItem)
                .ThenInclude(i => i!.Order)
            .Include(d => d.OrderItem)
                .ThenInclude(i => i!.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.Trim();
            query = query.Where(d =>
                d.SerialNumber.Contains(s) ||
                (d.OrderItem != null && d.OrderItem.Order != null && (d.OrderItem.Order.OrderCode.Contains(s) || d.OrderItem.Order.CustomerName.Contains(s) || d.OrderItem.Order.PhoneNumber.Contains(s))));
        }

        var devices = await query.OrderByDescending(d => d.DeliveredAt).ToListAsync();

        return devices.Select(d => new SoldDeviceDto
        {
            DeviceId = d.DeviceId,
            OrderItemId = d.OrderItemId,
            OrderId = d.OrderItem?.OrderId ?? 0,
            OrderCode = d.OrderItem?.Order?.OrderCode ?? "",
            CustomerName = d.OrderItem?.Order?.CustomerName ?? "",
            PhoneNumber = d.OrderItem?.Order?.PhoneNumber ?? "",
            SerialNumber = d.SerialNumber,
            ProductName = d.OrderItem?.ProductName ?? d.OrderItem?.Product?.ProductName ?? "Laptop",
            ColorName = d.OrderItem?.ColorName,
            SKU = d.OrderItem?.Variant?.SKU,
            ThumbnailUrl = d.OrderItem?.ThumbnailUrl ?? d.OrderItem?.Product?.ThumbnailUrl,
            DeliveredAt = d.DeliveredAt,
            WarrantyStartDate = d.WarrantyStartDate,
            WarrantyEndDate = d.WarrantyEndDate,
            WarrantyMonths = d.WarrantyMonths,
            IsUnderWarranty = DateTime.UtcNow <= d.WarrantyEndDate,
            Notes = d.Notes
        }).ToList();
    }

    private async Task<ServiceTicketDetailDto?> MapToDetailDtoAsync(int ticketId)
    {
        var t = await _db.ServiceTickets
            .AsNoTracking()
            .Include(x => x.Service)
            .Include(x => x.SoldDevice)
                .ThenInclude(d => d!.OrderItem)
            .Include(x => x.Quotes)
            .Include(x => x.Histories)
            .FirstOrDefaultAsync(x => x.TicketId == ticketId);

        if (t == null) return null;
        return MapEntityToDetailDto(t);
    }

    private static ServiceTicketDetailDto MapEntityToDetailDto(ServiceTicket t)
    {
        var isStore = t.SoldDevice != null;
        var deviceName = isStore
            ? (t.SoldDevice?.OrderItem?.ProductName ?? "Laptop đã mua")
            : (t.ExternalDeviceModel ?? "Thiết bị ngoài");

        var serial = isStore
            ? (t.SoldDevice?.SerialNumber ?? "")
            : (t.ExternalSerialNumber ?? "Chưa rõ serial");

        return new ServiceTicketDetailDto
        {
            TicketId = t.TicketId,
            TicketCode = t.TicketCode,
            TicketType = t.TicketType,
            Status = t.Status,
            CustomerName = t.CustomerName,
            PhoneNumber = t.PhoneNumber,
            Email = t.Email,
            Address = t.Address,
            SoldDeviceId = t.SoldDeviceId,
            DeviceDisplayName = deviceName,
            SerialNumber = serial,
            IsStoreDevice = isStore,
            IsUnderWarranty = t.SoldDevice?.IsUnderWarranty ?? false,
            WarrantyEndDate = t.SoldDevice?.WarrantyEndDate,
            ServiceId = t.ServiceId,
            ServiceCode = t.Service?.ServiceCode,
            ServiceName = t.Service?.ServiceName,
            EstimatedCost = t.Service?.EstimatedCost ?? 0,
            EstimatedTime = t.Service?.EstimatedTime,
            FaultDescription = t.FaultDescription,
            PhysicalCondition = t.PhysicalCondition,
            Accessories = t.Accessories,
            RejectReason = t.RejectReason,
            AssignedStaff = t.AssignedStaff,
            FinalCost = t.FinalCost,
            ServiceWarrantyUntil = t.ServiceWarrantyUntil,
            CreatedAt = t.CreatedAt,
            ReceivedAt = t.ReceivedAt,
            CompletedAt = t.CompletedAt,
            ReturnedAt = t.ReturnedAt,
            Quotes = t.Quotes.OrderByDescending(q => q.CreatedAt).Select(q => new ServiceQuoteDto
            {
                QuoteId = q.QuoteId,
                TicketId = q.TicketId,
                Description = q.Description,
                Amount = q.Amount,
                CustomerDecision = q.CustomerDecision,
                DecisionAt = q.DecisionAt,
                Notes = q.Notes,
                CreatedAt = q.CreatedAt
            }).ToList(),
            Histories = t.Histories.OrderByDescending(h => h.ActionAt).Select(h => new ServiceTicketHistoryDto
            {
                HistoryId = h.HistoryId,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                ActionBy = h.ActionBy,
                ActionAt = h.ActionAt,
                Notes = h.Notes
            }).ToList()
        };
    }
}
