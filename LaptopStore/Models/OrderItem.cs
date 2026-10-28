using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho một dòng sản phẩm trong Đơn hàng (Order Item)
/// Lưu lại bản chụp giá (Price Snapshot) và cấu hình tại thời điểm khách mua
/// </summary>
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

    /// <summary>
    /// Tên sản phẩm tại thời điểm mua (tránh bị đổi tên trong DB sau này)
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Cấu hình đã chọn tại lúc mua (vd: "RAM 16GB • SSD 512GB")
    /// </summary>
    [StringLength(255)]
    public string? SelectedSpecs { get; set; }

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Đơn giá bán tại thời điểm khách đặt hàng
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 500000000)]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Số lượng sản phẩm khách đặt mua
    /// </summary>
    [Required]
    [Range(1, 1000, ErrorMessage = "Số lượng mua phải lớn hơn 0")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Thành tiền = UnitPrice * Quantity
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
}
