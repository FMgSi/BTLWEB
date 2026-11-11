using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class ServiceTicketHistory
{
    [Key]
    public int HistoryId { get; set; }

    [Required]
    public int TicketId { get; set; }

    [ForeignKey("TicketId")]
    public ServiceTicket? Ticket { get; set; }

    [StringLength(50)]
    public string? PreviousStatus { get; set; }

    [Required]
    [StringLength(50)]
    public string NewStatus { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ActionBy { get; set; } = string.Empty;

    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Notes { get; set; }
}
