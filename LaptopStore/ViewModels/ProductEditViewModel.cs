using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LaptopStore.ViewModels;

public class ProductEditViewModel : IValidatableObject
{
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên sản phẩm từ 2 đến 200 ký tự")]
    [Display(Name = "Tên sản phẩm")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thương hiệu hợp lệ")]
    [Display(Name = "Thương hiệu")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phân khúc / danh mục")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục hợp lệ")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá bán")]
    [Range(0, 500000000, ErrorMessage = "Giá niêm yết phải lớn hơn hoặc bằng 0đ")]
    [Display(Name = "Giá niêm yết")]
    public decimal Price { get; set; }

    [Range(0, 500000000, ErrorMessage = "Giá khuyến mãi phải lớn hơn hoặc bằng 0đ")]
    [Display(Name = "Giá khuyến mãi")]
    public decimal? DiscountPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho")]
    [Range(0, 100000, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Số lượng tồn kho")]
    public int StockQuantity { get; set; }

    [Display(Name = "Mô tả sản phẩm")]
    public string? Description { get; set; }

    public string? ExistingThumbnail { get; set; }

    [Display(Name = "Tải ảnh sản phẩm")]
    public IFormFile? UploadedImage { get; set; }

    // Cấu hình chi tiết (Hardware Specs)
    [Display(Name = "Vi xử lý (CPU)")]
    public string? CPU { get; set; }

    [Display(Name = "Bộ nhớ RAM")]
    public string? RAM { get; set; }

    [Display(Name = "Ổ cứng (SSD/HDD)")]
    public string? Storage { get; set; }

    [Display(Name = "Card đồ hoạ (VGA/GPU)")]
    public string? GPU { get; set; }

    [Display(Name = "Kích thước màn hình (inch)")]
    [Range(10, 25, ErrorMessage = "Kích thước màn hình từ 10 đến 25 inch")]
    public decimal? ScreenSizeInch { get; set; }

    [Display(Name = "Tần số quét màn hình (Hz)")]
    [Range(30, 500, ErrorMessage = "Tần số quét từ 30Hz đến 500Hz")]
    public int? RefreshRateHz { get; set; }

    [Display(Name = "Trọng lượng (kg)")]
    [Range(0.5, 10, ErrorMessage = "Trọng lượng máy từ 0.5kg đến 10kg")]
    public decimal? WeightKg { get; set; }

    [Display(Name = "Dung lượng pin (Wh)")]
    [Range(20, 200, ErrorMessage = "Dung lượng pin từ 20Wh đến 200Wh")]
    public int? BatteryWh { get; set; }

    [Display(Name = "Hệ điều hành")]
    public string? OS { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiscountPrice.HasValue)
        {
            if (DiscountPrice.Value < 0)
            {
                yield return new ValidationResult(
                    "Giá khuyến mãi không được âm.",
                    new[] { nameof(DiscountPrice) }
                );
            }
            if (DiscountPrice.Value > Price)
            {
                yield return new ValidationResult(
                    "Giá khuyến mãi không được lớn hơn giá gốc niêm yết.",
                    new[] { nameof(DiscountPrice) }
                );
            }
        }

        if (UploadedImage != null && UploadedImage.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = System.IO.Path.GetExtension(UploadedImage.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                yield return new ValidationResult(
                    "Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .webp",
                    new[] { nameof(UploadedImage) }
                );
            }

            if (UploadedImage.Length > 5 * 1024 * 1024)
            {
                yield return new ValidationResult(
                    "Dung lượng ảnh không được vượt quá 5MB",
                    new[] { nameof(UploadedImage) }
                );
            }
        }
    }
}