using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class InstallmentContract
{
    [Key]
    public int ContractId { get; set; }

    [Required]
    [StringLength(50)]
    public string ContractCode { get; set; } = string.Empty;

    [Required]
    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [Required]
    [Range(3, 12)]
    public int TermMonths { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal PrepayPercent { get; set; } = 30.00m;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrepayAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalFinancedAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal InterestRate { get; set; } = 0.00m;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyInstallmentAmount { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Chờ trả trước";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ActivatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public ICollection<InstallmentSchedule> Schedules { get; set; } = new List<InstallmentSchedule>();
}
