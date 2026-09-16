using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LaptopStore.ViewModels;

public class ProductEditViewModel
{
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được trống")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chọn hãng")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Chọn danh mục")]
    public int CategoryId { get; set; }

    [Required]
    [Range(1000, 500000000, ErrorMessage = "Giá hợp lệ từ 1,000đ")]
    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Description { get; set; }
    public string? ExistingThumbnail { get; set; }

    [Display(Name = "Tải ảnh sản phẩm")]
    public IFormFile? UploadedImage { get; set; }
}