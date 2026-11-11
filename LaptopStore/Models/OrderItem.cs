using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class OrderItem
{
    [Key]
    public int OrderItemId { get; set; }

    [Required]
    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    public int? VariantId { get; set; }

    [ForeignKey("VariantId")]
    public ProductVariant? Variant { get; set; }

    [StringLength(100)]
    public string? ColorName { get; set; }

    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? SelectedSpecs { get; set; }

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 500000000)]
    public decimal UnitPrice { get; set; }

    [Required]
    [Range(1, 1000, ErrorMessage = "Số lượng mua phải lớn hơn 0")]
    public int Quantity { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
}
