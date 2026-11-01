using System.ComponentModel.DataAnnotations;

namespace LaptopStore.ViewModels;

public class ColorDto
{
    public int ColorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? HexCode { get; set; }
    public bool IsActive { get; set; }
    public int VariantCount { get; set; }
}

public class ProductVariantDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public string? HexCode { get; set; }
    public int RamGB { get; set; }
    public int StorageGB { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsAvailable => IsActive && StockQuantity > 0;
}

public class CreateProductVariantViewModel
{
    [Required]
    public int ProductId { get; set; }
    public string? ProductName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn màu sắc")]
    public int ColorId { get; set; }

    [Required(ErrorMessage = "Dung lượng RAM là bắt buộc")]
    [Range(4, 256, ErrorMessage = "RAM từ 4GB đến 256GB")]
    public int RamGB { get; set; } = 16;

    [Required(ErrorMessage = "Dung lượng ổ cứng là bắt buộc")]
    [Range(128, 8192, ErrorMessage = "Ổ cứng từ 128GB đến 8192GB")]
    public int StorageGB { get; set; } = 512;

    [Required(ErrorMessage = "Mã SKU quản lý kho là bắt buộc")]
    [StringLength(100)]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
    [Range(0, 100000, ErrorMessage = "Tồn kho không được âm")]
    public int StockQuantity { get; set; } = 1;

    [Range(0, 1000000000)]
    public decimal? Price { get; set; }

    [Range(0, 1000000000)]
    public decimal? DiscountPrice { get; set; }

    public string? ImageUrl { get; set; }
}

public class AdjustVariantStockViewModel
{
    [Required]
    public int VariantId { get; set; }
    public string? SKU { get; set; }
    public string? VariantDescription { get; set; }
    public int CurrentStock { get; set; }

    [Required]
    [Range(0, 100000, ErrorMessage = "Tồn kho không được âm")]
    public int NewQuantity { get; set; }

    public int NewStockQuantity
    {
        get => NewQuantity;
        set => NewQuantity = value;
    }

    [StringLength(255)]
    public string? Reason { get; set; } = "Cập nhật tồn kho";
}

public class UpdateVariantImageViewModel
{
    [Required]
    public int VariantId { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }
}
