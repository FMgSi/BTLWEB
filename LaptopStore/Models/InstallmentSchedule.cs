using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class InstallmentSchedule
{
    [Key]
    public int ScheduleId { get; set; }

    [Required]
    public int ContractId { get; set; }

    [ForeignKey("ContractId")]
    public InstallmentContract? Contract { get; set; }

    [Required]
    public int PeriodNumber { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; } = 0;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Chưa thanh toán";

    public DateTime? PaidAt { get; set; }

    public int? PaymentId { get; set; }

    [ForeignKey("PaymentId")]
    public Payment? Payment { get; set; }

    [StringLength(100)]
    public string? CollectedBy { get; set; }
}
