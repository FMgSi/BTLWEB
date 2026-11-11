using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models;

public class Category
{
    
    [Key]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Tên thể loại / danh mục không được để trống")]
    [StringLength(100, ErrorMessage = "Tên danh mục không vượt quá 100 ký tự")]
    [Display(Name = "Tên thể loại / danh mục")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(255)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
