using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho đợt báo giá chi phí sửa chữa / thay thế linh kiện gửi cho khách hàng duyệt
/// </summary>
public class ServiceQuote
{
    [Key]
    public int QuoteId { get; set; }

    [Required]
    public int TicketId { get; set; }

    [ForeignKey("TicketId")]
    public ServiceTicket? Ticket { get; set; }

    /// <summary>
    /// Nội dung công việc và linh kiện thay thế (ví dụ: Thay thế chip IC nguồn bo mạch chủ)
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Số tiền báo giá
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Quyết định của khách hàng: 'Chờ duyệt', 'Đồng ý', 'Từ chối'
    /// </summary>
    [Required]
    [StringLength(50)]
    public string CustomerDecision { get; set; } = "Chờ duyệt";

    /// <summary>
    /// Thời điểm khách hàng đưa ra quyết định duyệt hoặc từ chối
    /// </summary>
    public DateTime? DecisionAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
