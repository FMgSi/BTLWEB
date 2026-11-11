using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class ServiceTicket
{
    [Key]
    public int TicketId { get; set; }

    [Required]
    [StringLength(50)]
    public string TicketCode { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string TicketType { get; set; } = "Bảo hành";

    public int? ServiceId { get; set; }

    [ForeignKey("ServiceId")]
    public ServiceCatalogItem? Service { get; set; }

    public int? SoldDeviceId { get; set; }

    [ForeignKey("SoldDeviceId")]
    public SoldDevice? SoldDevice { get; set; }

    public int? UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Required]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    [StringLength(200)]
    public string? ExternalDeviceModel { get; set; }

    [StringLength(100)]
    public string? ExternalSerialNumber { get; set; }

    [Required]
    public string FaultDescription { get; set; } = string.Empty;

    [StringLength(500)]
    public string? PhysicalCondition { get; set; }

    [StringLength(255)]
    public string? Accessories { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Chờ tiếp nhận";

    [StringLength(500)]
    public string? RejectReason { get; set; }

    [StringLength(100)]
    public string? AssignedStaff { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FinalCost { get; set; } = 0;

    public DateTime? ServiceWarrantyUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public ICollection<ServiceQuote> Quotes { get; set; } = new List<ServiceQuote>();

    public ICollection<ServiceTicketHistory> Histories { get; set; } = new List<ServiceTicketHistory>();
}
