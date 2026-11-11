using System.Collections.Generic;
using System.Threading.Tasks;
using LaptopStore.Models;
using LaptopStore.ViewModels;

namespace LaptopStore.Services;

public interface IDiscountService
{
    
    Task<List<DiscountDto>> GetAllDiscountsAsync(string? search = null, string? status = null, string? discountType = null);

    Task<List<CustomerPromotionDto>> GetActivePromotionsForStorefrontAsync();

    Task<ValidateDiscountResult> ValidateDiscountForCartAsync(string code, decimal subTotal, List<CreateOrderItemRequest> items, string? phone = null, int? userId = null);

    Task<(bool Success, string Message, Discount? Discount, decimal DiscountAmount)> ValidateAndCalculateDiscountAsync(
        string code, decimal subTotal, List<CreateOrderItemRequest> items, string? phone = null, int? userId = null);

    Task<(bool Success, string Message, DiscountDto? Discount)> CreateDiscountAsync(CreateDiscountInput input, string createdBy);

    Task<(bool Success, string Message)> ToggleDiscountStatusAsync(int discountId, string updatedBy);

    Task<(bool Success, string Message)> DeleteDiscountAsync(int discountId, string deletedBy);
}
