using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Thực thể Mã giảm giá / Chương trình khuyến mãi (Voucher) lưu trong CSDL SQL Server
/// </summary>
public class Discount
{
    [Key]
    public int DiscountId { get; set; }

    /// <summary>
    /// Mã voucher duy nhất viết hoa (Ví dụ: LAPTOP2026, GAMING500K, APPLE, WELCOME)
    /// </summary>
    [Required(ErrorMessage = "Mã giảm giá không được để trống")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tiêu đề chương trình khuyến mãi
    /// </summary>
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết thể lệ khuyến mãi
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Loại giảm giá: "Percentage" (Theo %) hoặc "FixedAmount" (Số tiền cố định)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string DiscountType { get; set; } = "Percentage";

    /// <summary>
    /// Giá trị giảm: Số % (ví dụ: 10 cho 10%) hoặc số tiền (ví dụ: 500000 cho 500.000đ)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1000000000)]
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Số tiền giảm tối đa nếu là phần trăm (null nếu không áp dụng trần)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxDiscountAmount { get; set; }

    /// <summary>
    /// Giá trị đơn hàng tối thiểu để được áp dụng mã
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1000000000)]
    public decimal MinOrderAmount { get; set; } = 0;

    /// <summary>
    /// Thời điểm bắt đầu hiệu lực (UTC)
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời điểm kết thúc hiệu lực (UTC) - null nếu vô thời hạn
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Giới hạn tổng số lượt sử dụng toàn hệ thống (null nếu không giới hạn)
    /// </summary>
    public int? UsageLimit { get; set; }

    /// <summary>
    /// Số lượt mã đã được áp dụng thành công trong các đơn hàng
    /// </summary>
    public int UsedCount { get; set; } = 0;

    /// <summary>
    /// Phạm vi áp dụng: "All", "GamingOnly", "OfficeOnly", "AppleOnly", "FirstOrderOnly"
    /// </summary>
    [StringLength(50)]
    public string ApplicableScope { get; set; } = "All";

    /// <summary>
    /// Trạng thái kích hoạt của quản trị viên (true: bật, false: ngừng áp dụng)
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

