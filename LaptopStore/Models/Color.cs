using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho Danh mục Màu sắc laptop
/// Bảng ánh xạ: Colors
/// </summary>
public class Color
{
    [Key]
    public int ColorId { get; set; }

    /// <summary>
    /// Tên màu sắc hiển thị (vd: Bạc, Đen, Xám không gian, Trắng, Xanh navy)
    /// </summary>
    [Required(ErrorMessage = "Tên màu sắc không được để trống")]
    [StringLength(100, ErrorMessage = "Tên màu tối đa 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mã màu HEX để hiển thị chấm màu hoặc swatch trên giao diện (vd: #C0C0C0, #000000, #4B5563)
    /// </summary>
    [StringLength(20)]
    public string? HexCode { get; set; }

    /// <summary>
    /// Trạng thái hoạt động (1: Đang kinh doanh, 0: Ngừng áp dụng)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Danh sách các phiên bản laptop sử dụng màu này
    /// </summary>
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

