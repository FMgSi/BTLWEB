using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Services;

public class DiscountService : IDiscountService
{
    private readonly AppDbContext _db;

    public DiscountService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DiscountDto>> GetAllDiscountsAsync(string? search = null, string? status = null, string? discountType = null)
    {
        var now = DateTime.UtcNow;
        var query = _db.Discounts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(d => d.Code.ToLower().Contains(s) || d.Title.ToLower().Contains(s));
        }

        var rawList = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();

        var dtoList = rawList.Select(d =>
        {
            var (stat, badge) = ComputeStatus(d, now);

            string valText = d.DiscountType == "Percentage"
                ? $"{d.DiscountValue:0.##}%" + (d.MaxDiscountAmount.HasValue ? $" (Tối đa {FormatVnd(d.MaxDiscountAmount.Value)})" : "")
                : FormatVnd(d.DiscountValue);

            string condText = d.MinOrderAmount > 0
                ? $"Đơn từ {FormatVnd(d.MinOrderAmount)}"
                : "Không giới hạn đơn";

            if (d.ApplicableScope == "GamingOnly") condText += " • Laptop Gaming";
            else if (d.ApplicableScope == "OfficeOnly") condText += " • Laptop Văn phòng";
            else if (d.ApplicableScope == "AppleOnly") condText += " • Apple MacBook";
            else if (d.ApplicableScope == "FirstOrderOnly") condText += " • Đơn đầu tiên";

            string usageText = d.UsageLimit.HasValue
                ? $"{d.UsedCount} / {d.UsageLimit.Value}"
                : $"{d.UsedCount} / Không giới hạn";

            string expiryText = d.EndDate.HasValue
                ? $"{d.StartDate:dd/MM/yyyy} - {d.EndDate.Value:dd/MM/yyyy}"
                : "Không thời hạn";

            return new DiscountDto
            {
                DiscountId = d.DiscountId,
                Code = d.Code,
                Title = d.Title,
                Description = d.Description,
                DiscountType = d.DiscountType,
                DiscountValue = d.DiscountValue,
                MaxDiscountAmount = d.MaxDiscountAmount,
                MinOrderAmount = d.MinOrderAmount,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                UsageLimit = d.UsageLimit,
                UsedCount = d.UsedCount,
                ApplicableScope = d.ApplicableScope,
                IsActive = d.IsActive,
                DiscountValueText = valText,
                ConditionText = condText,
                UsageText = usageText,
                ExpiryText = expiryText,
                Status = stat,
                StatusBadgeClass = badge
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            if (status == "active")
            {
                dtoList = dtoList.Where(d => d.Status == "Đang hoạt động").ToList();
            }
            else if (status == "upcoming")
            {
                dtoList = dtoList.Where(d => d.Status == "Chưa bắt đầu").ToList();
            }
            else if (status == "expired")
            {
                dtoList = dtoList.Where(d => d.Status == "Hết hạn" || d.Status == "Hết lượt dùng" || d.Status == "Đã ngừng").ToList();
            }
        }

        if (!string.IsNullOrWhiteSpace(discountType) && discountType != "all")
        {
            if (discountType == "percent")
            {
                dtoList = dtoList.Where(d => d.DiscountType == "Percentage").ToList();
            }
            else if (discountType == "fixed")
            {
                dtoList = dtoList.Where(d => d.DiscountType == "FixedAmount").ToList();
            }
        }

        return dtoList;
    }

    public async Task<List<CustomerPromotionDto>> GetActivePromotionsForStorefrontAsync()
    {
        var now = DateTime.UtcNow;
        var activeDiscounts = await _db.Discounts
            .AsNoTracking()
            .Where(d => d.IsActive &&
                        d.StartDate <= now &&
                        (!d.EndDate.HasValue || d.EndDate.Value >= now) &&
                        (!d.UsageLimit.HasValue || d.UsedCount < d.UsageLimit.Value))
            .OrderByDescending(d => d.DiscountValue)
            .ToListAsync();

        return activeDiscounts.Select(d =>
        {
            string valBadge = d.DiscountType == "Percentage"
                ? $"Giảm {d.DiscountValue:0.##}%"
                : $"Giảm {FormatVnd(d.DiscountValue)}";

            string scopeBadge = d.ApplicableScope switch
            {
                "GamingOnly" => "GAMING",
                "OfficeOnly" => "VĂN PHÒNG",
                "AppleOnly" => "APPLE",
                "FirstOrderOnly" => "THÀNH VIÊN MỚI",
                _ => "TOÀN BỘ SẢN PHẨM"
            };

            return new CustomerPromotionDto
            {
                DiscountId = d.DiscountId,
                Code = d.Code,
                Title = d.Title,
                Description = d.Description,
                DiscountType = d.DiscountType,
                DiscountValue = d.DiscountValue,
                MaxDiscountAmount = d.MaxDiscountAmount,
                MinOrderAmount = d.MinOrderAmount,
                EndDateFormatted = d.EndDate.HasValue ? d.EndDate.Value.ToString("dd/MM/yyyy") : "Không giới hạn",
                ValueBadge = valBadge,
                ScopeBadge = scopeBadge,
                ApplicableScope = d.ApplicableScope,
                IsHot = d.DiscountValue >= 10 || d.DiscountValue >= 1000000
            };
        }).ToList();
    }

    public async Task<ValidateDiscountResult> ValidateDiscountForCartAsync(
        string code, decimal subTotal, List<CreateOrderItemRequest> items, string? phone = null, int? userId = null)
    {
        var (success, message, discount, discountAmount) = await ValidateAndCalculateDiscountAsync(code, subTotal, items, phone, userId);

        if (!success || discount == null)
        {
            decimal? minAmount = discount?.MinOrderAmount;
            decimal? needMore = (minAmount.HasValue && minAmount.Value > subTotal) ? minAmount.Value - subTotal : null;

            return new ValidateDiscountResult
            {
                IsValid = false,
                Message = message,
                Code = code.Trim().ToUpper(),
                SubTotal = subTotal,
                TotalPayable = subTotal,
                DiscountAmount = 0,
                MinOrderAmount = minAmount,
                NeedMoreAmount = needMore
            };
        }

        return new ValidateDiscountResult
        {
            IsValid = true,
            Message = message,
            Code = discount.Code,
            Title = discount.Title,
            DiscountType = discount.DiscountType,
            DiscountAmount = discountAmount,
            SubTotal = subTotal,
            TotalPayable = Math.Max(0, subTotal - discountAmount),
            MinOrderAmount = discount.MinOrderAmount
        };
    }

    public async Task<(bool Success, string Message, Discount? Discount, decimal DiscountAmount)> ValidateAndCalculateDiscountAsync(
        string code, decimal subTotal, List<CreateOrderItemRequest> items, string? phone = null, int? userId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return (false, "Vui lòng nhập mã giảm giá.", null, 0);
        }

        var normalizedCode = code.Trim().ToUpper();
        var discount = await _db.Discounts.FirstOrDefaultAsync(d => d.Code == normalizedCode);

        if (discount == null)
        {
            return (false, $"Mã giảm giá '{normalizedCode}' không tồn tại trên hệ thống.", null, 0);
        }

        if (!discount.IsActive)
        {
            return (false, $"Mã giảm giá '{normalizedCode}' hiện đang tạm ngừng áp dụng.", discount, 0);
        }

        var now = DateTime.UtcNow;
        if (now < discount.StartDate)
        {
            return (false, $"Mã giảm giá '{normalizedCode}' chỉ bắt đầu có hiệu lực từ ngày {discount.StartDate:dd/MM/yyyy}.", discount, 0);
        }

        if (discount.EndDate.HasValue && now > discount.EndDate.Value)
        {
            return (false, $"Mã giảm giá '{normalizedCode}' đã hết hạn sử dụng vào ngày {discount.EndDate.Value:dd/MM/yyyy}.", discount, 0);
        }

        if (discount.UsageLimit.HasValue && discount.UsedCount >= discount.UsageLimit.Value)
        {
            return (false, $"Mã giảm giá '{normalizedCode}' đã hết lượt sử dụng ({discount.UsedCount}/{discount.UsageLimit.Value}).", discount, 0);
        }

        if (subTotal < discount.MinOrderAmount)
        {
            decimal needMore = discount.MinOrderAmount - subTotal;
            return (false, $"Cần mua thêm {FormatVnd(needMore)} để dùng mã này (Đơn hàng tối thiểu {FormatVnd(discount.MinOrderAmount)}).", discount, 0);
        }

        if (discount.ApplicableScope != "All" && items != null && items.Any())
        {
            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            if (discount.ApplicableScope == "GamingOnly")
            {
                bool hasGaming = products.Any(p =>
                    (p.Category != null && p.Category.CategoryName.Contains("Gaming", StringComparison.OrdinalIgnoreCase)) ||
                    p.ProductName.Contains("Gaming", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("ROG", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("TUF", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("Legion", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("Victus", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("Predator", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("Nitro", StringComparison.OrdinalIgnoreCase));

                if (!hasGaming)
                {
                    return (false, "Mã giảm giá này chỉ áp dụng cho đơn hàng có Laptop Gaming.", discount, 0);
                }
            }
            else if (discount.ApplicableScope == "OfficeOnly")
            {
                bool hasOffice = products.Any(p =>
                    (p.Category != null && (p.Category.CategoryName.Contains("Văn phòng", StringComparison.OrdinalIgnoreCase) ||
                                            p.Category.CategoryName.Contains("Học tập", StringComparison.OrdinalIgnoreCase))) ||
                    p.ProductName.Contains("Inspiron", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("Zenbook", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("Vivobook", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("Swift", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("Pavilion", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("ThinkBook", StringComparison.OrdinalIgnoreCase));

                if (!hasOffice)
                {
                    return (false, "Mã giảm giá này chỉ áp dụng cho đơn hàng có Laptop Văn phòng & Học tập.", discount, 0);
                }
            }
            else if (discount.ApplicableScope == "AppleOnly")
            {
                bool hasApple = products.Any(p =>
                    (p.Brand != null && p.Brand.BrandName.Contains("Apple", StringComparison.OrdinalIgnoreCase)) ||
                    p.ProductName.Contains("MacBook", StringComparison.OrdinalIgnoreCase) ||
                    p.ProductName.Contains("Apple", StringComparison.OrdinalIgnoreCase));

                if (!hasApple)
                {
                    return (false, "Mã giảm giá này chỉ áp dụng cho các dòng Apple MacBook.", discount, 0);
                }
            }
        }

        if (discount.ApplicableScope == "FirstOrderOnly")
        {
            bool hasPreviousOrders = false;
            if (userId.HasValue && userId.Value > 0)
            {
                hasPreviousOrders = await _db.Orders.AnyAsync(o => o.UserId == userId.Value && o.OrderStatus != "Đã hủy");
            }

            if (!hasPreviousOrders && !string.IsNullOrWhiteSpace(phone))
            {
                var cleanPhone = phone.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
                hasPreviousOrders = await _db.Orders.AnyAsync(o => o.PhoneNumber == cleanPhone && o.OrderStatus != "Đã hủy");
            }

            if (hasPreviousOrders)
            {
                return (false, "Mã ưu đãi này chỉ dành riêng cho đơn hàng đầu tiên của khách hàng mới.", discount, 0);
            }
        }

        decimal calculatedDiscount;
        if (discount.DiscountType == "Percentage")
        {
            calculatedDiscount = Math.Round(subTotal * (discount.DiscountValue / 100m), 0, MidpointRounding.AwayFromZero);
            if (discount.MaxDiscountAmount.HasValue && calculatedDiscount > discount.MaxDiscountAmount.Value)
            {
                calculatedDiscount = discount.MaxDiscountAmount.Value;
            }
        }
        else
        {
            calculatedDiscount = Math.Min(discount.DiscountValue, subTotal);
        }

        calculatedDiscount = Math.Min(calculatedDiscount, subTotal);
        return (true, $"Áp dụng thành công mã {discount.Code}! Giảm {FormatVnd(calculatedDiscount)}.", discount, calculatedDiscount);
    }

    public async Task<(bool Success, string Message, DiscountDto? Discount)> CreateDiscountAsync(CreateDiscountInput input, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(input.Code))
        {
            return (false, "Mã giảm giá không được để trống.", null);
        }

        var normalizedCode = input.Code.Trim().ToUpper();
        if (await _db.Discounts.AnyAsync(d => d.Code == normalizedCode))
        {
            return (false, $"Mã voucher '{normalizedCode}' đã tồn tại trên hệ thống. Vui lòng chọn mã khác.", null);
        }

        var discount = new Discount
        {
            Code = normalizedCode,
            Title = input.Title.Trim(),
            Description = input.Description?.Trim(),
            DiscountType = input.DiscountType == "FixedAmount" || input.DiscountType == "fixed" ? "FixedAmount" : "Percentage",
            DiscountValue = input.DiscountValue,
            MaxDiscountAmount = input.MaxDiscountAmount,
            MinOrderAmount = input.MinOrderAmount,
            StartDate = input.StartDate ?? DateTime.UtcNow,
            EndDate = input.EndDate,
            UsageLimit = input.UsageLimit,
            UsedCount = 0,
            ApplicableScope = input.ApplicableScope ?? "All",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Discounts.Add(discount);
        await _db.SaveChangesAsync();

        var dtoList = await GetAllDiscountsAsync(search: normalizedCode);
        return (true, $"Đã tạo thành công mã giảm giá '{discount.Code}'!", dtoList.FirstOrDefault());
    }

    public async Task<(bool Success, string Message)> ToggleDiscountStatusAsync(int discountId, string updatedBy)
    {
        var discount = await _db.Discounts.FindAsync(discountId);
        if (discount == null)
        {
            return (false, "Không tìm thấy mã giảm giá.");
        }

        discount.IsActive = !discount.IsActive;
        await _db.SaveChangesAsync();

        var actionText = discount.IsActive ? "kích hoạt hoạt động" : "ngừng áp dụng";
        return (true, $"Đã {actionText} mã giảm giá '{discount.Code}'.");
    }

    public async Task<(bool Success, string Message)> DeleteDiscountAsync(int discountId, string deletedBy)
    {
        var discount = await _db.Discounts.FindAsync(discountId);
        if (discount == null)
        {
            return (false, "Không tìm thấy mã giảm giá.");
        }

        if (discount.UsedCount > 0)
        {
            discount.IsActive = false;
            await _db.SaveChangesAsync();
            return (true, $"Mã '{discount.Code}' đã được áp dụng trong {discount.UsedCount} đơn hàng. Để bảo vệ dữ liệu lịch sử hóa đơn, hệ thống đã tự động chuyển sang trạng thái 'Ngừng áp dụng' thay vì xóa hoàn toàn.");
        }

        _db.Discounts.Remove(discount);
        await _db.SaveChangesAsync();
        return (true, $"Đã xóa vĩnh viễn mã giảm giá '{discount.Code}'.");
    }

    private static (string Status, string BadgeClass) ComputeStatus(Discount d, DateTime now)
    {
        if (!d.IsActive)
        {
            return ("Đã ngừng", "badge-out-of-stock");
        }

        if (now < d.StartDate)
        {
            return ("Chưa bắt đầu", "badge-low-stock");
        }

        if (d.EndDate.HasValue && now > d.EndDate.Value)
        {
            return ("Hết hạn", "badge-out-of-stock");
        }

        if (d.UsageLimit.HasValue && d.UsedCount >= d.UsageLimit.Value)
        {
            return ("Hết lượt dùng", "badge-out-of-stock");
        }

        return ("Đang hoạt động", "badge-in-stock");
    }

    private static string FormatVnd(decimal amount)
    {
        return amount.ToString("#,##0", CultureInfo.GetCultureInfo("vi-VN")) + "đ";
    }
}
