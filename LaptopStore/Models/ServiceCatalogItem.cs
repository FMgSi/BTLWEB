using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Danh mục dịch vụ Sửa chữa & Bảo hành (Seeded từ QuanLyBanLaptop)
/// Phân loại: 'Sửa chữa' (có phí tham khảo, bảo hành dịch vụ) hoặc 'Bảo hành' (phí 0đ, điều kiện hãng)
/// </summary>
public class ServiceCatalogItem
{
    [Key]
    public int ServiceId { get; set; }

    /// <summary>
    /// Mã dịch vụ duy nhất (ví dụ: DV-SC01, DV-BH01)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ServiceCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Phân loại: 'Sửa chữa' hoặc 'Bảo hành'
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ServiceType { get; set; } = "Sửa chữa";

    /// <summary>
    /// Điều kiện áp dụng (đối với bảo hành) hoặc triệu chứng nhận biết (đối với sửa chữa)
    /// </summary>
    [StringLength(500)]
    public string? ApplicableConditions { get; set; }

    /// <summary>
    /// Giá tham khảo / chi phí ước tính (Bảo hành = 0đ, Sửa chữa >= 0)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedCost { get; set; } = 0;

    /// <summary>
    /// Thời gian xử lý dự kiến (ví dụ: '30 - 45 phút', '1 - 2 ngày')
    /// </summary>
    [StringLength(100)]
    public string? EstimatedTime { get; set; }

    /// <summary>
    /// Thời gian bảo hành dịch vụ sau sửa chữa (tháng). Ví dụ: 1 tháng, 6 tháng, 12 tháng.
    /// </summary>
    public int ServiceWarrantyMonths { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}
