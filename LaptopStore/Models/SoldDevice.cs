using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class SoldDevice
{
    [Key]
    public int DeviceId { get; set; }

    [Required]
    public int OrderItemId { get; set; }

    [ForeignKey("OrderItemId")]
    public OrderItem? OrderItem { get; set; }

    [Required]
    [StringLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;

    public DateTime WarrantyStartDate { get; set; } = DateTime.UtcNow;

    public DateTime WarrantyEndDate { get; set; }

    public int WarrantyMonths { get; set; } = 12;

    [StringLength(500)]
    public string? Notes { get; set; }

    public ICollection<ServiceTicket> ServiceTickets { get; set; } = new List<ServiceTicket>();

    [NotMapped]
    public bool IsUnderWarranty => DateTime.UtcNow <= WarrantyEndDate;
}
