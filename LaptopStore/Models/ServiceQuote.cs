using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class ServiceQuote
{
    [Key]
    public int QuoteId { get; set; }

    [Required]
    public int TicketId { get; set; }

    [ForeignKey("TicketId")]
    public ServiceTicket? Ticket { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(50)]
    public string CustomerDecision { get; set; } = "Chờ duyệt";

    public DateTime? DecisionAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
