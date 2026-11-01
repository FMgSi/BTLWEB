using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho từng chiếc máy thực tế đã bán và giao cho khách hàng (quản lý theo Serial duy nhất)
/// Dùng để quản lý nguồn gốc, chủ sở hữu và hạn bảo hành chính xác cho từng máy.
/// </summary>
public class SoldDevice
{
    [Key]
    public int DeviceId { get; set; }

    [Required]
    public int OrderItemId { get; set; }

    [ForeignKey("OrderItemId")]
    public OrderItem? OrderItem { get; set; }

    /// <summary>
    /// Số Serial duy nhất của chiếc máy (ví dụ: SN-DELL-8829103)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm bàn giao máy cho khách
    /// </summary>
    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ngày bắt đầu tính bảo hành (mặc định = Ngày giao hàng)
    /// </summary>
    public DateTime WarrantyStartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ngày kết thúc thời hạn bảo hành chính hãng
    /// </summary>
    public DateTime WarrantyEndDate { get; set; }

    /// <summary>
    /// Thời gian bảo hành (tháng), mặc định 12 tháng hoặc 24 tháng theo thông số sản phẩm
    /// </summary>
    public int WarrantyMonths { get; set; } = 12;

    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Danh sách các phiếu bảo hành/sửa chữa đã từng tiếp nhận cho thiết bị này
    /// </summary>
    public ICollection<ServiceTicket> ServiceTickets { get; set; } = new List<ServiceTicket>();

    /// <summary>
    /// Kiểm tra máy còn trong thời hạn bảo hành hay không
    /// </summary>
    [NotMapped]
    public bool IsUnderWarranty => DateTime.UtcNow <= WarrantyEndDate;
}

