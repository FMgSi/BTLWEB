using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models;


public class Category
{
    public int CategoryId { get; set; }
    [Required]
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}