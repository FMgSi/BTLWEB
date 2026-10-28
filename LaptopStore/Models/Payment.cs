using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho giao dịch thanh toán của đơn hàng (Payments)
/// </summary>
public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [Required]
    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "COD";

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Mã tham chiếu giao dịch (vd: Mã chuẩn chi VietQR, VNPAY Transaction No)
    /// </summary>
    [StringLength(100)]
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Trạng thái thanh toán: "Chờ thanh toán", "Thành công", "Thất bại"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string PaymentStatus { get; set; } = "Chờ thanh toán";

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? Note { get; set; }
}
