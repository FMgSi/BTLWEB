using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class ProductVariant
{
    [Key]
    public int VariantId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [Required]
    public int ColorId { get; set; }

    [ForeignKey("ColorId")]
    public Color? Color { get; set; }

    [Required]
    [Range(4, 256)]
    public int RamGB { get; set; }

    [Required]
    [Range(128, 8192)]
    public int StorageGB { get; set; }

    [Required(ErrorMessage = "Mã SKU không được để trống")]
    [StringLength(100)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [Range(0, 100000, ErrorMessage = "Tồn kho không được là số âm")]
    public int StockQuantity { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPrice { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public decimal EffectivePrice
    {
        get
        {
            if (DiscountPrice.HasValue && DiscountPrice.Value > 0)
                return DiscountPrice.Value;
            if (Price.HasValue && Price.Value > 0)
                return Price.Value;
            return Product?.DiscountPrice ?? Product?.Price ?? 0;
        }
    }
}
