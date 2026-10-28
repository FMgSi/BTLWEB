using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers.Api;

/// <summary>
/// Web API Quản lý và Tiếp nhận Đơn đặt hàng từ khách hàng (React Frontend)
/// Đường dẫn gốc: /api/orders
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersApiController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersApiController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// POST /api/orders
    /// Tiếp nhận đơn đặt hàng từ giỏ hàng của khách hàng
    /// Kiểm tra hợp lệ, trừ tồn kho nguyên tử và lưu thông tin vào SQL Server
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join("; ", errors) });
        }

        // Lấy UserId từ Session nếu khách đã đăng nhập
        int? userId = null;
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
        {
            userId = parsedId;
        }

        var (success, message, order) = await _orderService.CreateOrderAsync(request, userId);

        if (!success || order == null)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new
        {
            success = true,
            message,
            orderId = order.OrderId,
            orderCode = order.OrderCode,
            order
        });
    }

    /// <summary>
    /// GET /api/orders/code/{orderCode}
    /// Tra cứu thông tin đơn hàng theo mã đơn (vd: PC20261028-1001)
    /// </summary>
    [HttpGet("code/{orderCode}")]
    public async Task<IActionResult> GetByOrderCode(string orderCode)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return BadRequest(new { success = false, message = "Vui lòng cung cấp mã đơn hàng." });
        }

        var order = await _orderService.GetOrderByCodeAsync(orderCode);
        if (order == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy đơn hàng với mã: " + orderCode });
        }

        return Ok(new { success = true, order });
    }

    /// <summary>
    /// GET /api/orders/my-orders
    /// Lấy danh sách lịch sử đơn hàng của tài khoản đang đăng nhập
    /// </summary>
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để xem lịch sử đơn hàng." });
        }

        var orders = await _orderService.GetUserOrdersAsync(userId);
        return Ok(new { success = true, orders });
    }
}
