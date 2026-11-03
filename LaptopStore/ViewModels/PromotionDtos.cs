using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaptopStore.ViewModels;

public class CreateDiscountInput
{
    [Required(ErrorMessage = "Mã voucher không được để trống")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Mã từ 3 đến 50 ký tự")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// "Percentage" hoặc "FixedAmount"
    /// </summary>
    [Required]
    public string DiscountType { get; set; } = "Percentage";

    [Range(1, 1000000000, ErrorMessage = "Giá trị giảm phải lớn hơn 0")]
    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public decimal MinOrderAmount { get; set; } = 0;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? UsageLimit { get; set; }

    public string ApplicableScope { get; set; } = "All";
}

public class ValidateDiscountRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã giảm giá")]
    public string Code { get; set; } = string.Empty;

    [Range(0, 1000000000)]
    public decimal SubTotal { get; set; }

    public string? PhoneNumber { get; set; }

    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class ValidateDiscountResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Title { get; set; }
    public string? DiscountType { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalPayable { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? NeedMoreAmount { get; set; }
}

public class CustomerPromotionDto
{
    public int DiscountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public string? EndDateFormatted { get; set; }
    public string ValueBadge { get; set; } = string.Empty;
    public string ScopeBadge { get; set; } = string.Empty;
    public string ApplicableScope { get; set; } = "All";
    public bool IsHot { get; set; }
}

