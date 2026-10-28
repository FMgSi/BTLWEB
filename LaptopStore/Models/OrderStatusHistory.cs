using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho lịch sử thay đổi trạng thái của Đơn hàng (Audit Trail / Status History)
/// </summary>
public class OrderStatusHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Note { get; set; }
}
