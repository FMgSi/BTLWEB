using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho Thương hiệu / Hãng sản xuất laptop (vd: ASUS, Dell, Apple, Lenovo, HP, MSI, Acer)
/// Bảng ánh xạ: Brands
/// </summary>
public class Brand
{
    /// <summary>
    /// Mã định danh thương hiệu (Khóa chính)
    /// </summary>
    [Key]
    public int BrandId { get; set; }

    /// <summary>
    /// Tên thương hiệu
    /// </summary>
    [Required(ErrorMessage = "Tên thương hiệu / hãng không được để trống")]
    [StringLength(100, ErrorMessage = "Tên thương hiệu không quá 100 ký tự")]
    [Display(Name = "Tên thương hiệu / Hãng")]
    public string BrandName { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn logo của hãng
    /// </summary>
    [StringLength(255)]
    [Display(Name = "Logo hãng")]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Giới thiệu ngắn về thương hiệu
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Mô tả / Xuất xứ")]
    public string? Description { get; set; }

    /// <summary>
    /// Danh sách các sản phẩm laptop thuộc thương hiệu này (Quan hệ 1 - Nhiều)
    /// </summary>
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
