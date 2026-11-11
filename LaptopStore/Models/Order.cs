using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class Order
{
    [Key]
    public int OrderId { get; set; }

    [Required]
    [StringLength(50)]
    public string OrderCode { get; set; } = string.Empty;

    [StringLength(100)]
    public string? IdempotencyKey { get; set; }

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

    [StringLength(50)]
    [Display(Name = "Mã giảm giá áp dụng")]
    public string? DiscountCode { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10000000000)]
    [Display(Name = "Tổng thanh toán")]
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Hình thức mua hàng")]
    public string PurchaseMode { get; set; } = "Full";

    [Required]
    [StringLength(50)]
    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD";

    [Required]
    [StringLength(50)]
    [Display(Name = "Trạng thái thanh toán")]
    public string PaymentStatus { get; set; } = "Chờ thanh toán";

    [Required]
    [StringLength(50)]
    [Display(Name = "Trạng thái đơn hàng")]
    public string OrderStatus { get; set; } = "Đang xử lý";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();

    public InstallmentContract? InstallmentContract { get; set; }
}
