using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models;

public class Brand
{
    
    [Key]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Tên thương hiệu / hãng không được để trống")]
    [StringLength(100, ErrorMessage = "Tên thương hiệu không quá 100 ký tự")]
    [Display(Name = "Tên thương hiệu / Hãng")]
    public string BrandName { get; set; } = string.Empty;

    [StringLength(255)]
    [Display(Name = "Logo hãng")]
    public string? LogoUrl { get; set; }

    [StringLength(500)]
    [Display(Name = "Mô tả / Xuất xứ")]
    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
