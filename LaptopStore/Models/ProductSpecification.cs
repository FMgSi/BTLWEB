using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho bảng cấu hình phần cứng chi tiết của một chiếc Laptop
/// Bảng ánh xạ: ProductSpecifications (Quan hệ 1 - 1 với bảng Products)
/// </summary>
public class ProductSpecification
{
    /// <summary>
    /// Khóa chính đồng thời là Khóa ngoại liên kết trực tiếp tới ProductId của bảng Products
    /// </summary>
    [Key, ForeignKey("Product")]
    public int ProductId { get; set; }

    /// <summary>
    /// Tên vi xử lý CPU (vd: Intel Core i7-13650HX, AMD Ryzen 7 7840HS, Apple M3)
    /// </summary>
    public string? CPU { get; set; }

    /// <summary>
    /// Dung lượng bộ nhớ RAM tính theo GB (vd: 8, 16, 32, 64)
    /// </summary>
    public int RamGB { get; set; }

    /// <summary>
    /// Dung lượng ổ cứng lưu trữ tính theo GB (vd: 512, 1024)
    /// </summary>
    public int StorageGB { get; set; }

    /// <summary>
    /// Loại ổ cứng (vd: SSD NVMe PCIe 4.0)
    /// </summary>
    public string? StorageType { get; set; }

    /// <summary>
    /// Card đồ họa GPU (vd: NVIDIA RTX 4060 8GB, Intel Iris Xe)
    /// </summary>
    public string? GPU { get; set; }

    /// <summary>
    /// Kích thước đường chéo màn hình tính bằng Inch (vd: 13.3, 14.0, 15.6, 16.0, 17.3)
    /// Dùng cho chức năng lọc theo màn hình ở phía Frontend
    /// </summary>
    public decimal ScreenSizeInch { get; set; }

    /// <summary>
    /// Tần số quét màn hình tính bằng Hz (vd: 60, 120, 144, 165, 240)
    /// </summary>
    public int RefreshRateHz { get; set; }

    /// <summary>
    /// Trọng lượng thân máy tính theo kg (vd: 1.24, 1.85, 2.5)
    /// </summary>
    public decimal WeightKg { get; set; }

    /// <summary>
    /// Dung lượng pin tính theo Watt-giờ (Wh)
    /// </summary>
    public int BatteryWh { get; set; }

    /// <summary>
    /// Hệ điều hành cài sẵn (vd: Windows 11 Home, macOS Sonoma)
    /// </summary>
    public string? OS { get; set; }

    /// <summary>
    /// Thuộc tính điều hướng (Navigation Property) liên kết ngược lại Entity Product
    /// </summary>
    public Product? Product { get; set; }
}
