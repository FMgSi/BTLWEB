using System.ComponentModel.DataAnnotations;

namespace LaptopStore.ViewModels;

public class ServiceCatalogDto
{
    public int ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty; // 'Sửa chữa' | 'Bảo hành'
    public string? ApplicableConditions { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? EstimatedTime { get; set; }
    public int ServiceWarrantyMonths { get; set; }
    public bool IsActive { get; set; }
}

public class SoldDeviceDto
{
    public int DeviceId { get; set; }
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ColorName { get; set; }
    public string? SKU { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime DeliveredAt { get; set; }
    public DateTime WarrantyStartDate { get; set; }
    public DateTime WarrantyEndDate { get; set; }
    public int WarrantyMonths { get; set; }
    public bool IsUnderWarranty { get; set; }
    public string? Notes { get; set; }
}

public class CreateWarrantyTicketRequest
{
    [Required(ErrorMessage = "Vui lòng chọn thiết bị đã mua cần yêu cầu bảo hành")]
    public int SoldDeviceId { get; set; }

    [Required(ErrorMessage = "Họ tên người liên hệ không được để trống")]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [RegularExpression(@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$", ErrorMessage = "Số điện thoại không đúng định dạng di động Việt Nam")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    public string? Address { get; set; }

    public int? ServiceId { get; set; }

    [Required(ErrorMessage = "Vui lòng mô tả chi tiết lỗi hoặc triệu chứng")]
    [MinLength(10, ErrorMessage = "Mô tả lỗi tối thiểu 10 ký tự")]
    public string FaultDescription { get; set; } = string.Empty;

    public string? Accessories { get; set; }
}

public class CreateRepairTicketRequest
{
    [Required(ErrorMessage = "Họ tên người liên hệ không được để trống")]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [RegularExpression(@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$", ErrorMessage = "Số điện thoại không đúng định dạng di động Việt Nam")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    public string? Address { get; set; }

    public int? ServiceId { get; set; }

    public string DeviceSource { get; set; } = "External"; // "Purchased" hoặc "External"

    public int? SoldDeviceId { get; set; }

    public string? ExternalDeviceModel { get; set; }

    public string? ExternalSerialNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng mô tả chi tiết lỗi hoặc triệu chứng")]
    [MinLength(10, ErrorMessage = "Mô tả lỗi tối thiểu 10 ký tự")]
    public string FaultDescription { get; set; } = string.Empty;

    public string? Accessories { get; set; }
}

public class ServiceQuoteDto
{
    public int QuoteId { get; set; }
    public int TicketId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CustomerDecision { get; set; } = "Chờ duyệt";
    public DateTime? DecisionAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ServiceTicketHistoryDto
{
    public int HistoryId { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string ActionBy { get; set; } = string.Empty;
    public DateTime ActionAt { get; set; }
    public string? Notes { get; set; }
}

public class ServiceTicketDetailDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string TicketType { get; set; } = string.Empty; // 'Bảo hành' | 'Sửa chữa'
    public string Status { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }

    // Thông tin thiết bị
    public int? SoldDeviceId { get; set; }
    public string DeviceDisplayName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public bool IsStoreDevice { get; set; }
    public bool IsUnderWarranty { get; set; }
    public DateTime? WarrantyEndDate { get; set; }

    // Hạng mục dịch vụ
    public int? ServiceId { get; set; }
    public string? ServiceCode { get; set; }
    public string? ServiceName { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? EstimatedTime { get; set; }

    // Chi tiết xử lý
    public string FaultDescription { get; set; } = string.Empty;
    public string? PhysicalCondition { get; set; }
    public string? Accessories { get; set; }
    public string? RejectReason { get; set; }
    public string? AssignedStaff { get; set; }
    public decimal FinalCost { get; set; }
    public DateTime? ServiceWarrantyUntil { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    public List<ServiceQuoteDto> Quotes { get; set; } = new();
    public List<ServiceTicketHistoryDto> Histories { get; set; } = new();
}

public class CustomerDecisionRequest
{
    [Required]
    public int QuoteId { get; set; }

    [Required]
    [RegularExpression(@"^(Đồng ý|Từ chối)$", ErrorMessage = "Quyết định chỉ có thể là 'Đồng ý' hoặc 'Từ chối'")]
    public string Decision { get; set; } = string.Empty;

    public string? Notes { get; set; }
    public string? PhoneNumber { get; set; }
}

public class AssignSerialsRequest
{
    public int OrderId { get; set; }
    public List<OrderItemSerialsInput> Items { get; set; } = new();
}

public class OrderItemSerialsInput
{
    public int OrderItemId { get; set; }
    public List<string> SerialNumbers { get; set; } = new();
}
