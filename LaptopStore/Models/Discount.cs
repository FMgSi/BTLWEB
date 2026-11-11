using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class Discount
{
    [Key]
    public int DiscountId { get; set; }

    [Required(ErrorMessage = "Mã giảm giá không được để trống")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string DiscountType { get; set; } = "Percentage";

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1000000000)]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxDiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1000000000)]
    public decimal MinOrderAmount { get; set; } = 0;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime? EndDate { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; } = 0;

    [StringLength(50)]
    public string ApplicableScope { get; set; } = "All";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
