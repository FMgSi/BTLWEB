using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class ProductImage
{
    [Key]
    public int ImageId { get; set; }

    [Required]
    public int ProductId { get; set; }

    public int? ColorId { get; set; }

    [Required]
    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Range(1, 4)]
    public int DisplayOrder { get; set; } = 1;

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("ColorId")]
    public Color? Color { get; set; }
}
