using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaptopStore.Models;

namespace LaptopStore.Models;


public class ProductImage
{
    [Key]
    public int ImageId { get; set; }
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 1;
    public Product? Product { get; set; }
}