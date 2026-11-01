using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho một Phiên bản cụ thể của laptop (Tổ hợp: Mẫu máy + Màu sắc + RAM + SSD)
/// Đơn vị quản lý tồn kho và bán hàng thực tế (Sellable Unit)
/// Bảng ánh xạ: ProductVariants
/// </summary>
public class ProductVariant
{
    [Key]
    public int VariantId { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới sản phẩm gốc (Mẫu laptop)
    /// </summary>
    [Required]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới màu sắc
    /// </summary>
    [Required]
    public int ColorId { get; set; }

    [ForeignKey("ColorId")]
    public Color? Color { get; set; }

    /// <summary>
    /// Dung lượng RAM tính bằng GB (vd: 8, 16, 32, 64)
    /// </summary>
    [Required]
    [Range(4, 256)]
    public int RamGB { get; set; }

    /// <summary>
    /// Dung lượng ổ cứng lưu trữ tính bằng GB (vd: 256, 512, 1024, 2048)
    /// </summary>
    [Required]
    [Range(128, 8192)]
    public int StorageGB { get; set; }

    /// <summary>
    /// Mã SKU quản lý kho duy nhất (vd: DELL-INSP15-SLV-16-512)
    /// </summary>
    [Required(ErrorMessage = "Mã SKU không được để trống")]
    [StringLength(100)]
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng tồn kho thực tế của riêng phiên bản này (luôn >= 0)
    /// </summary>
    [Required]
    [Range(0, 100000, ErrorMessage = "Tồn kho không được là số âm")]
    public int StockQuantity { get; set; } = 0;

    /// <summary>
    /// Giá bán riêng của phiên bản này (nếu để null thì kế thừa từ giá gốc Product.Price)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    /// <summary>
    /// Giá khuyến mãi riêng của phiên bản này
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPrice { get; set; }

    /// <summary>
    /// Đường dẫn ảnh riêng cho phiên bản màu này (nếu null thì dùng Product.ThumbnailUrl)
    /// </summary>
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Trạng thái mở bán của phiên bản (1: Đang mở bán, 0: Tạm ngừng bán)
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Helper: Lấy giá bán thực tế của phiên bản (ưu tiên giá riêng nếu > 0, fallback về giá sản phẩm)
    /// </summary>
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

