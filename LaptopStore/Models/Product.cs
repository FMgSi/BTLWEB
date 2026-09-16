using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho một sản phẩm Laptop trong cơ sở dữ liệu MySQL
/// Bảng ánh xạ: Products
/// </summary>
public class Product
{
    /// <summary>
    /// Khóa chính (Primary Key), tự động tăng
    /// </summary>
    [Key]
    public int ProductId { get; set; }

    /// <summary>
    /// Tên đầy đủ của laptop (vd: ASUS ROG Strix G16, MacBook Air M3)
    /// </summary>
    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [StringLength(200, ErrorMessage = "Tên không quá 200 ký tự")]
    [Display(Name = "Tên sản phẩm")]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Khóa ngoại (Foreign Key) liên kết đến bảng Brands
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn hãng sản xuất")]
    [Display(Name = "Hãng sản xuất")]
    public int BrandId { get; set; }

    [ForeignKey("BrandId")]
    public Brand? Brand { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết đến bảng Categories (Danh mục như Gaming, Văn phòng, Đồ họa)
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }

    /// <summary>
    /// Giá bán niêm yết (gốc) của sản phẩm
    /// </summary>
    [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
    [Range(0, 500000000, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giá niêm yết")]
    public decimal Price { get; set; }

    /// <summary>
    /// Giá khuyến mãi (nếu có giảm giá)
    /// </summary>
    [Range(0, 500000000, ErrorMessage = "Giá khuyến mãi hợp lệ")]
    [Display(Name = "Giá khuyến mãi")]
    public decimal? DiscountPrice { get; set; }

    /// <summary>
    /// Số lượng sản phẩm còn trong kho
    /// </summary>
    [Required]
    [Range(0, 10000, ErrorMessage = "Số lượng tồn kho từ 0 đến 10,000")]
    [Display(Name = "Số lượng tồn")]
    public int StockQuantity { get; set; }

    /// <summary>
    /// Đường dẫn hình ảnh đại diện (vd: /images/products/asus-rog-strix-g16.png)
    /// </summary>
    [Display(Name = "Ảnh đại diện")]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Đoạn văn bản mô tả điểm nổi bật của máy
    /// </summary>
    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái kích hoạt (true = đang kinh doanh hiển thị trên web, false = ngừng bán)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Quan hệ 1-1 với bảng cấu hình chi tiết (CPU, RAM, Ổ cứng, Màn hình)
    /// </summary>
    public ProductSpecification? Specification { get; set; }

    /// <summary>
    /// Quan hệ 1-Nhiều với bộ sưu tập hình ảnh bổ sung
    /// </summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}