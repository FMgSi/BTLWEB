using System.ComponentModel.DataAnnotations;

namespace LaptopStore.ViewModels;

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên người nhận")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 100 ký tự")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Địa chỉ tối thiểu 5 ký tự")]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "COD"; // "COD", "VietQR", "Banking", "VNPAY"

    /// <summary>
    /// Hình thức mua hàng: "Full" (Trả toàn bộ), "Installment" (Trả góp)
    /// </summary>
    public string PurchaseMode { get; set; } = "Full";

    /// <summary>
    /// Kỳ hạn trả góp nếu chọn trả góp: 3, 6, 9 hoặc 12 tháng
    /// </summary>
    public int? TermMonths { get; set; }

    /// <summary>
    /// Khóa idempotency từ client chống tạo đơn hàng trùng lặp
    /// </summary>
    public string? IdempotencyKey { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Giỏ hàng phải có ít nhất 1 sản phẩm để đặt hàng")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    [Required]
    public int ProductId { get; set; }

    /// <summary>
    /// ID phiên bản cụ thể (Màu + RAM + SSD). Bắt buộc đối với các đơn hàng mới.
    /// </summary>
    public int? VariantId { get; set; }

    /// <summary>
    /// Tên màu đã chọn (vd: "Bạc", "Đen")
    /// </summary>
    public string? ColorName { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng mỗi sản phẩm từ 1 đến 100")]
    public int Quantity { get; set; } = 1;

    public string? SelectedSpecs { get; set; }
}

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PurchaseMode { get; set; } = "Full";
    public decimal? PrepayAmount { get; set; }
    public decimal? RemainingDebt { get; set; }
    public InstallmentContractDetailDto? InstallmentContract { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class OrderItemResponseDto
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string? ColorName { get; set; }
    public string? SKU { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SelectedSpecs { get; set; }
    public string? ThumbnailUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "Trạng thái mới không được để trống")]
    public string NewStatus { get; set; } = string.Empty;

    public string? Note { get; set; }
}
