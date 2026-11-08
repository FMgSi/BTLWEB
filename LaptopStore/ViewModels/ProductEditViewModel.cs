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

    [Display(Name = "Dung lượng RAM (GB)")]
    public int? RamGB { get; set; } = 16;

    [Display(Name = "Bộ nhớ RAM (Chuỗi)")]
    public string? RAM { get; set; }

    [Display(Name = "Dung lượng ổ cứng (GB)")]
    public int? StorageGB { get; set; } = 512;

    [Display(Name = "Loại ổ cứng")]
    public string? StorageType { get; set; } = "SSD NVMe";

    [Display(Name = "Ổ cứng (SSD/HDD - Chuỗi)")]
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

    /// <summary>
    /// ID màu đang được chọn để quản lý ảnh trong giao diện Chỉnh sửa
    /// </summary>
    public int? SelectedColorId { get; set; }

    /// <summary>
    /// URL ảnh hiện tại ở Ô 1 (Ảnh đại diện sản phẩm)
    /// </summary>
    public string? Slot1Url { get; set; }

    /// <summary>
    /// URL ảnh hiện tại ở Ô 2 (Ảnh phụ 1)
    /// </summary>
    public string? Slot2Url { get; set; }

    /// <summary>
    /// URL ảnh hiện tại ở Ô 3 (Ảnh phụ 2)
    /// </summary>
    public string? Slot3Url { get; set; }

    /// <summary>
    /// URL ảnh hiện tại ở Ô 4 (Ảnh phụ 3)
    /// </summary>
    public string? Slot4Url { get; set; }

    /// <summary>
    /// File ảnh tải lên cho Ô 1 (Ảnh đại diện của màu - Bắt buộc / Ưu tiên)
    /// </summary>
    public IFormFile? Slot1Image { get; set; }

    /// <summary>
    /// File ảnh tải lên cho Ô 2 (Ảnh phụ 1 của màu)
    /// </summary>
    public IFormFile? Slot2Image { get; set; }

    /// <summary>
    /// File ảnh tải lên cho Ô 3 (Ảnh phụ 2 của màu)
    /// </summary>
    public IFormFile? Slot3Image { get; set; }

    /// <summary>
    /// File ảnh tải lên cho Ô 4 (Ảnh phụ 3 của màu)
    /// </summary>
    public IFormFile? Slot4Image { get; set; }

    /// <summary>
    /// Cờ xóa ảnh ở Ô 2
    /// </summary>
    public bool DeleteSlot2 { get; set; }

    /// <summary>
    /// Cờ xóa ảnh ở Ô 3
    /// </summary>
    public bool DeleteSlot3 { get; set; }

    /// <summary>
    /// Cờ xóa ảnh ở Ô 4
    /// </summary>
    public bool DeleteSlot4 { get; set; }

    /// <summary>
    /// Danh sách màu và 4 slot ảnh của từng màu để hiển thị trong View
    /// </summary>
    public List<ProductColorImagesViewModel> ColorImagesList { get; set; } = new();

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

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        void ValidateImageFile(IFormFile? file, string memberName, string label, List<ValidationResult> errors)
        {
            if (file == null || file.Length == 0) return;
            var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                errors.Add(new ValidationResult(
                    $"{label}: Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .webp",
                    new[] { memberName }
                ));
            }
            if (file.Length > 5 * 1024 * 1024)
            {
                errors.Add(new ValidationResult(
                    $"{label}: Dung lượng ảnh không được vượt quá 5MB",
                    new[] { memberName }
                ));
            }
        }

        var fileErrors = new List<ValidationResult>();
        ValidateImageFile(UploadedImage, nameof(UploadedImage), "Ảnh chung", fileErrors);
        ValidateImageFile(Slot1Image, nameof(Slot1Image), "Ô 1 (Ảnh đại diện)", fileErrors);
        ValidateImageFile(Slot2Image, nameof(Slot2Image), "Ô 2 (Ảnh phụ 1)", fileErrors);
        ValidateImageFile(Slot3Image, nameof(Slot3Image), "Ô 3 (Ảnh phụ 2)", fileErrors);
        ValidateImageFile(Slot4Image, nameof(Slot4Image), "Ô 4 (Ảnh phụ 3)", fileErrors);

        foreach (var err in fileErrors)
        {
            yield return err;
        }
    }
}

/// <summary>
/// ViewModel chứa 4 ô ảnh cho một màu sắc của sản phẩm
/// </summary>
public class ProductColorImagesViewModel
{
    public int ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public string HexCode { get; set; } = "#CBD5E1";
    public string? Slot1Url { get; set; }
    public string? Slot2Url { get; set; }
    public string? Slot3Url { get; set; }
    public string? Slot4Url { get; set; }
}