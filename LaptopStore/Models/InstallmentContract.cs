using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho Hợp đồng mua trả góp Laptop
/// Bảng ánh xạ: InstallmentContracts
/// </summary>
public class InstallmentContract
{
    [Key]
    public int ContractId { get; set; }

    /// <summary>
    /// Mã hợp đồng trả góp (vd: HDTG20261101-9821)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ContractCode { get; set; } = string.Empty;

    /// <summary>
    /// Khóa ngoại liên kết 1-1 duy nhất tới Đơn hàng (Orders)
    /// </summary>
    [Required]
    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    /// <summary>
    /// Kỳ hạn trả góp: 3, 6, 9 hoặc 12 tháng
    /// </summary>
    [Required]
    [Range(3, 12)]
    public int TermMonths { get; set; }

    /// <summary>
    /// Tỷ lệ trả trước áp dụng (%): mặc định 30%
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal PrepayPercent { get; set; } = 30.00m;

    /// <summary>
    /// Số tiền trả trước phải thu (30% tổng tiền đơn)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrepayAmount { get; set; }

    /// <summary>
    /// Tổng số tiền còn lại trả qua các kỳ (70% tổng tiền đơn)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalFinancedAmount { get; set; }

    /// <summary>
    /// Lãi suất áp dụng (%): mặc định 0%
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal InterestRate { get; set; } = 0.00m;

    /// <summary>
    /// Số tiền mỗi kỳ cơ bản (các kỳ 1..n-1)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyInstallmentAmount { get; set; }

    /// <summary>
    /// Trạng thái hợp đồng: "Chờ trả trước", "Đang trả góp", "Đã tất toán", "Đã hủy"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Chờ trả trước";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ngày kích hoạt hợp đồng (thời điểm admin xác nhận đã nhận khoản trả trước)
    /// </summary>
    public DateTime? ActivatedAt { get; set; }

    /// <summary>
    /// Ngày hoàn thành tất toán hợp đồng (thu đủ kỳ cuối)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Danh sách các kỳ trả góp theo lịch (1..TermMonths)
    /// </summary>
    public ICollection<InstallmentSchedule> Schedules { get; set; } = new List<InstallmentSchedule>();
}
