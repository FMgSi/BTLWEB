using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models;

public class Color
{
    [Key]
    public int ColorId { get; set; }

    [Required(ErrorMessage = "Tên màu sắc không được để trống")]
    [StringLength(100, ErrorMessage = "Tên màu tối đa 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public string? HexCode { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
