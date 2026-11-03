using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho một hình ảnh của sản phẩm theo màu sắc và vị trí hiển thị (1-4)
/// Bảng ánh xạ: ProductImages
/// </summary>
public class ProductImage
{
    [Key]
    public int ImageId { get; set; }

    [Required]
    public int ProductId { get; set; }

    /// <summary>
    /// Mã màu sắc mà ảnh thuộc về (null nếu là ảnh chung/cũ chưa phân loại)
    /// </summary>
    public int? ColorId { get; set; }

    [Required]
    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Vị trí ô ảnh: 1 (Ảnh đại diện), 2 (Ảnh phụ 1), 3 (Ảnh phụ 2), 4 (Ảnh phụ 3)
    /// </summary>
    [Range(1, 4)]
    public int DisplayOrder { get; set; } = 1;

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("ColorId")]
    public Color? Color { get; set; }
}
