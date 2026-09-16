using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaptopStore.Models;

namespace LaptopStore.Models;

public class Product
{
    [Key]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [StringLength(200, ErrorMessage = "Tên không quá 200 ký tự")]
    [Display(Name = "Tên sản phẩm")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn hãng sản xuất")]
    [Display(Name = "Hãng sản xuất")]
    public int BrandId { get; set; }

    [ForeignKey("BrandId")]
    public Brand? Brand { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }

    [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
    [Range(0, 500000000, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giá niêm yết")]
    public decimal Price { get; set; }

    [Range(0, 500000000, ErrorMessage = "Giá khuyến mãi hợp lệ")]
    [Display(Name = "Giá khuyến mãi")]
    public decimal? DiscountPrice { get; set; }

    [Required]
    [Range(0, 10000, ErrorMessage = "Số lượng tồn kho từ 0 đến 10,000")]
    [Display(Name = "Số lượng tồn")]
    public int StockQuantity { get; set; }

    [Display(Name = "Ảnh đại diện")]
    public string? ThumbnailUrl { get; set; }

    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ProductSpecification? Specification { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}