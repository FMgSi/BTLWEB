using LaptopStore.ViewModels;

namespace LaptopStore.Services;

public interface IOrderService
{
    Task<(bool Success, string Message, OrderResponseDto? Order)> CreateOrderAsync(CreateOrderRequest req, int? userId = null);

    Task<PagedResult<OrderResponseDto>> GetOrdersAsync(string? search, string? status, int page, int pageSize);

    Task<OrderResponseDto?> GetOrderByCodeAsync(string orderCode);

    Task<List<OrderResponseDto>> GetUserOrdersAsync(int userId);

    Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string newStatus, string? note = null, string? changedBy = null);

    Task<AdminDashboardViewModel> GetDashboardStatsAsync();
}
