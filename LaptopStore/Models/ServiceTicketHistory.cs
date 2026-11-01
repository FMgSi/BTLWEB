using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Nhật ký hành trình xử lý phiếu dịch vụ bảo hành / sửa chữa
/// </summary>
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

    /// <summary>
    /// Người thực hiện thao tác (Nhân viên tiếp nhận / Kỹ thuật viên / Khách hàng duyệt báo giá)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ActionBy { get; set; } = string.Empty;

    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Notes { get; set; }
}
