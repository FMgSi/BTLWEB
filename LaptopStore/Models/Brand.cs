using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models;

public class Brand
{
    public int BrandId { get; set; }
    [Required]
    public string BrandName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
