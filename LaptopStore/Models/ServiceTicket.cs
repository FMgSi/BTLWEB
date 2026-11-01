using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho một phiếu tiếp nhận Dịch vụ & Bảo hành (ServiceTicket)
/// Dùng chung quy trình tiếp nhận, theo dõi và bàn giao cho cả 2 loại:
/// 1. Bảo hành: Yêu cầu gắn với một SoldDevice đã bán tại cửa hàng và còn hạn.
/// 2. Sửa chữa: Áp dụng cho mọi khách hàng, cho phép máy ngoài danh mục.
/// </summary>
public class ServiceTicket
{
    [Key]
    public int TicketId { get; set; }

    /// <summary>
    /// Mã phiếu duy nhất (ví dụ: TC-20261101-0001, BH-001, SC-001)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TicketCode { get; set; } = string.Empty;

    /// <summary>
    /// Phân loại yêu cầu: 'Bảo hành' hoặc 'Sửa chữa'
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TicketType { get; set; } = "Bảo hành";

    /// <summary>
    /// Hạng mục dịch vụ lựa chọn trong ServiceCatalog (nếu có)
    /// </summary>
    public int? ServiceId { get; set; }

    [ForeignKey("ServiceId")]
    public ServiceCatalogItem? Service { get; set; }

    /// <summary>
    /// Liên kết máy đã bán từ cửa hàng (Bắt buộc nếu TicketType == 'Bảo hành', tùy chọn với 'Sửa chữa')
    /// </summary>
    public int? SoldDeviceId { get; set; }

    [ForeignKey("SoldDeviceId")]
    public SoldDevice? SoldDevice { get; set; }

    /// <summary>
    /// Tài khoản khách hàng tạo yêu cầu (nếu đã đăng nhập)
    /// </summary>
    public int? UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    // Thông tin người liên hệ / mang máy đến
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

    // Thông tin máy ngoài (áp dụng khi khách mang máy ngoài hoặc chưa liên kết SoldDevice)
    [StringLength(200)]
    public string? ExternalDeviceModel { get; set; }

    [StringLength(100)]
    public string? ExternalSerialNumber { get; set; }

    /// <summary>
    /// Mô tả chi tiết triệu chứng / lỗi khách phản ánh
    /// </summary>
    [Required]
    public string FaultDescription { get; set; } = string.Empty;

    /// <summary>
    /// Tình trạng ngoại quan khi nhân viên nhận máy (vỏ trầy, nứt góc, màn hình xước...)
    /// </summary>
    [StringLength(500)]
    public string? PhysicalCondition { get; set; }

    /// <summary>
    /// Phụ kiện đi kèm máy khi gửi (Sạc, cáp, túi, chuột...)
    /// </summary>
    [StringLength(255)]
    public string? Accessories { get; set; }

    /// <summary>
    /// Trạng thái phiếu:
    /// 'Chờ tiếp nhận' -> 'Đã tiếp nhận' -> 'Đang kiểm tra' -> 'Chờ duyệt báo giá' -> 'Đang xử lý' -> 'Chờ trả máy' -> 'Đã trả máy' | 'Đã hủy/Từ chối'
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Chờ tiếp nhận";

    /// <summary>
    /// Lý do từ chối bảo hành (ví dụ: Rơi vỡ màn hình, tem bị rách, máy ẩm nước)
    /// </summary>
    [StringLength(500)]
    public string? RejectReason { get; set; }

    /// <summary>
    /// Nhân viên kỹ thuật phụ trách kiểm tra / xử lý
    /// </summary>
    [StringLength(100)]
    public string? AssignedStaff { get; set; }

    /// <summary>
    /// Tổng chi phí khách phải thanh toán (Bảo hành hợp lệ = 0đ; Sửa chữa = Số tiền báo giá đã duyệt)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal FinalCost { get; set; } = 0;

    /// <summary>
    /// Thời hạn bảo hành của dịch vụ sau khi sửa chữa xong
    /// </summary>
    public DateTime? ServiceWarrantyUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Danh sách các đợt báo giá gửi cho khách duyệt
    /// </summary>
    public ICollection<ServiceQuote> Quotes { get; set; } = new List<ServiceQuote>();

    /// <summary>
    /// Lịch sử thay đổi trạng thái và nhật ký xử lý
    /// </summary>
    public ICollection<ServiceTicketHistory> Histories { get; set; } = new List<ServiceTicketHistory>();
}
