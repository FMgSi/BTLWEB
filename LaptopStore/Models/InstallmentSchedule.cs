using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho một kỳ thanh toán trong hợp đồng trả góp
/// Bảng ánh xạ: InstallmentSchedules
/// </summary>
public class InstallmentSchedule
{
    [Key]
    public int ScheduleId { get; set; }

    [Required]
    public int ContractId { get; set; }

    [ForeignKey("ContractId")]
    public InstallmentContract? Contract { get; set; }

    /// <summary>
    /// Số thứ tự kỳ thanh toán (1, 2, ... TermMonths)
    /// </summary>
    [Required]
    public int PeriodNumber { get; set; }

    /// <summary>
    /// Ngày đến hạn của kỳ trả góp
    /// Ban đầu là ngày dự kiến, sau khi kích hoạt hợp đồng sẽ chốt chính xác = ActivatedAt + PeriodNumber tháng
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Số tiền phải thu của kỳ này
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Số tiền thực tế đã thanh toán cho kỳ này
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; } = 0;

    /// <summary>
    /// Trạng thái kỳ trả góp: "Chưa thanh toán", "Đã thanh toán", "Quá hạn"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Chưa thanh toán";

    /// <summary>
    /// Ngày thực tế thu tiền kỳ này
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới giao dịch thanh toán trong bảng Payments (nếu đã thanh toán)
    /// </summary>
    public int? PaymentId { get; set; }

    [ForeignKey("PaymentId")]
    public Payment? Payment { get; set; }

    /// <summary>
    /// Tên nhân viên / quản trị viên thực hiện thu tiền
    /// </summary>
    [StringLength(100)]
    public string? CollectedBy { get; set; }
}
