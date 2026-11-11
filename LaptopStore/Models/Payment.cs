using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

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

    [StringLength(100)]
    public string? TransactionReference { get; set; }

    [Required]
    [StringLength(50)]
    public string PaymentStatus { get; set; } = "Chờ thanh toán";

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(50)]
    public string PaymentType { get; set; } = "Full";

    public int? ScheduleId { get; set; }

    [ForeignKey("ScheduleId")]
    public InstallmentSchedule? Schedule { get; set; }

    [StringLength(100)]
    public string? ConfirmedBy { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }
}
