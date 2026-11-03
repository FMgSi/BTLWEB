using System.Collections.Generic;
using System.Threading.Tasks;
using LaptopStore.Models;
using LaptopStore.ViewModels;

namespace LaptopStore.Services;

/// <summary>
/// Service quản lý nghiệp vụ Khuyến mãi & Mã giảm giá (Single Source of Truth trên SQL Server)
/// </summary>
public interface IDiscountService
{
    /// <summary>
    /// Lấy danh sách mã giảm giá cho trang Quản trị (Admin/Staff), tính toán trạng thái động theo thời gian thực
    /// </summary>
    Task<List<DiscountDto>> GetAllDiscountsAsync(string? search = null, string? status = null, string? discountType = null);

    /// <summary>
    /// Lấy danh sách các ưu đãi đang hiệu lực để hiển thị cho khách hàng xem trên website
    /// </summary>
    Task<List<CustomerPromotionDto>> GetActivePromotionsForStorefrontAsync();

    /// <summary>
    /// Kiểm tra tính hợp lệ và trả về kết quả chi tiết kèm lý do cho client giỏ hàng
    /// </summary>
    Task<ValidateDiscountResult> ValidateDiscountForCartAsync(string code, decimal subTotal, List<CreateOrderItemRequest> items, string? phone = null, int? userId = null);

    /// <summary>
    /// Xác thực và tính toán số tiền giảm giá chính xác phục vụ cho việc tạo đơn hàng tại OrderService
    /// </summary>
    Task<(bool Success, string Message, Discount? Discount, decimal DiscountAmount)> ValidateAndCalculateDiscountAsync(
        string code, decimal subTotal, List<CreateOrderItemRequest> items, string? phone = null, int? userId = null);

    /// <summary>
    /// Tạo mới một chương trình khuyến mãi
    /// </summary>
    Task<(bool Success, string Message, DiscountDto? Discount)> CreateDiscountAsync(CreateDiscountInput input, string createdBy);

    /// <summary>
    /// Bật / Tắt (Ngừng áp dụng) mã giảm giá
    /// </summary>
    Task<(bool Success, string Message)> ToggleDiscountStatusAsync(int discountId, string updatedBy);

    /// <summary>
    /// Xóa mã giảm giá (Nếu đã có đơn sử dụng -> tự động chuyển sang Ngừng áp dụng để bảo vệ lịch sử hóa đơn)
    /// </summary>
    Task<(bool Success, string Message)> DeleteDiscountAsync(int discountId, string deletedBy);
}

