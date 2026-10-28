using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho một Đơn hàng trong hệ thống bán lẻ Laptop
/// Bảng ánh xạ: Orders
/// </summary>
public class Order
{
    [Key]
    public int OrderId { get; set; }

    /// <summary>
    /// Mã đơn hàng hiển thị cho khách và quản trị (vd: PC20261028-9821)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OrderCode { get; set; } = string.Empty;

    /// <summary>
    /// Khóa Idempotency chống tạo đơn trùng lặp khi client gửi lại request
    /// </summary>
    [StringLength(100)]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới tài khoản User (nếu khách đã đăng nhập)
    /// Cho phép null nếu khách mua hàng không đăng nhập (Guest Checkout)
    /// </summary>
    public int? UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Required(ErrorMessage = "Họ tên khách hàng không được để trống")]
    [StringLength(100)]
    [Display(Name = "Họ tên khách hàng")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại nhận hàng là bắt buộc")]
    [StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Địa chỉ nhận hàng là bắt buộc")]
    [StringLength(255)]
    [Display(Name = "Địa chỉ nhận hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Ghi chú đơn hàng")]
    public string? Notes { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10000000000)]
    [Display(Name = "Tạm tính")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10000000000)]
    [Display(Name = "Giảm giá")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10000000000)]
    [Display(Name = "Tổng thanh toán")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Phương thức thanh toán: "COD", "VietQR", "Banking", "VNPAY"
    /// </summary>
    [Required]
    [StringLength(50)]
    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD";

    /// <summary>
    /// Trạng thái thanh toán: "Chờ thanh toán", "Đã thanh toán", "Thất bại", "Đã hoàn tiền"
    /// </summary>
    [Required]
    [StringLength(50)]
    [Display(Name = "Trạng thái thanh toán")]
    public string PaymentStatus { get; set; } = "Chờ thanh toán";

    /// <summary>
    /// Trạng thái xử lý đơn hàng: "Đang xử lý", "Đã xác nhận", "Đang giao", "Hoàn thành", "Đã hủy"
    /// </summary>
    [Required]
    [StringLength(50)]
    [Display(Name = "Trạng thái đơn hàng")]
    public string OrderStatus { get; set; } = "Đang xử lý";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Danh sách sản phẩm trong đơn hàng (Chi tiết đơn hàng)
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    /// <summary>
    /// Lịch sử các giao dịch thanh toán của đơn
    /// </summary>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>
    /// Lịch sử thay đổi trạng thái đơn hàng
    /// </summary>
    public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
}
