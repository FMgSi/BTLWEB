using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho Danh mục phân loại laptop (vd: Laptop Gaming, Laptop Học tập - Văn phòng, Laptop Đồ họa, Laptop Mỏng nhẹ)
/// Bảng ánh xạ: Categories
/// </summary>
public class Category
{
    /// <summary>
    /// Mã danh mục (Khóa chính)
    /// </summary>
    [Key]
    public int CategoryId { get; set; }

    /// <summary>
    /// Tên phân loại danh mục
    /// </summary>
    [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết về phân khúc laptop này
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Danh sách các sản phẩm laptop thuộc danh mục này (Quan hệ 1 - Nhiều)
    /// </summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}