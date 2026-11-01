using System;
using System.Linq;
using System.Threading.Tasks;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers.Api;

/// <summary>
/// Web API Quản lý và Tiếp nhận Đơn đặt hàng từ khách hàng (React Frontend)
/// Đường dẫn gốc chuẩn hóa: /api/orders
/// Alias tương thích ngược: /api/OrdersApi
/// </summary>
[ApiController]
[Route("api/orders")]
[Route("api/[controller]")]
public class OrdersApiController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IInstallmentService _installmentService;

    public OrdersApiController(IOrderService orderService, IInstallmentService installmentService)
    {
        _orderService = orderService;
        _installmentService = installmentService;
    }

    /// <summary>
    /// GET /api/orders/installment-preview?amount=20000000&termMonths=6
    /// Xem trước bảng tính phương án trả góp: số tiền trả trước 30%, lịch từng kỳ và ngày đến hạn
    /// </summary>
    [HttpGet("installment-preview")]
    public IActionResult PreviewInstallmentPlan([FromQuery] decimal amount, [FromQuery] int termMonths = 6)
    {
        if (amount < 1000000)
        {
            return BadRequest(new { success = false, message = "Đơn hàng tối thiểu từ 1.000.000đ để áp dụng chương trình trả góp 0%." });
        }

        if (termMonths != 3 && termMonths != 6 && termMonths != 9 && termMonths != 12)
        {
            termMonths = 6;
        }

        var plan = _installmentService.CalculatePlan(amount, termMonths);
        return Ok(new { success = true, plan });
    }

    /// <summary>
    /// POST /api/orders
    /// Tiếp nhận đơn đặt hàng từ giỏ hàng của khách hàng
    /// Kiểm tra hợp lệ, trừ tồn kho nguyên tử chống bán vượt kho và lưu thông tin vào SQL Server
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join("; ", errors) });
        }

        // Lấy UserId từ Session nếu khách đã đăng nhập (kiểm tra an toàn tránh exception khi Session chưa kích hoạt)
        int? userId = null;
        try
        {
            if (HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>()?.Session != null)
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }
            }
        }
        catch { }

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
            totalAmount = order.TotalAmount,
            order
        });
    }

    /// <summary>
    /// GET /api/orders/code/{orderCode}?phone=0901234567
    /// Tra cứu thông tin đơn hàng theo mã đơn
    /// Bảo mật thông tin cá nhân: Chỉ trả về thông tin đầy đủ nếu là Quản trị viên, Chủ sở hữu đơn,
    /// hoặc khách hàng cung cấp đúng số điện thoại đặt hàng.
    /// </summary>
    [HttpGet("code/{orderCode}")]
    public async Task<IActionResult> GetByOrderCode(string orderCode, [FromQuery] string? phone = null)
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

        // 1. Quản trị viên
        bool isAdmin = User.IsInRole("Admin") || HttpContext.Session.GetString("Role") == "Admin";

        // 2. Khách hàng đã đăng nhập
        int? currentUserId = null;
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
        {
            currentUserId = uid;
        }

        bool isOwner = false;
        if (isAdmin)
        {
            isOwner = true;
        }
        else if (currentUserId.HasValue)
        {
            var userOrders = await _orderService.GetUserOrdersAsync(currentUserId.Value);
            isOwner = userOrders.Any(o => o.OrderCode == order.OrderCode);
        }

        // 3. Khách vãng lai (không đăng nhập hoặc chưa chứng minh quyền sở hữu)
        if (!isOwner)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                // Che giấu (mask) thông tin cá nhân nhạy cảm
                MaskSensitiveCustomerInfo(order);
                return Ok(new { success = true, order, isMasked = true, message = "Thông tin cá nhân đã được bảo vệ. Vui lòng cung cấp số điện thoại đặt hàng để xem chi tiết đầy đủ." });
            }

            var normalizedOrderPhone = NormalizePhone(order.PhoneNumber);
            var normalizedInputPhone = NormalizePhone(phone);

            if (normalizedOrderPhone != normalizedInputPhone)
            {
                return BadRequest(new { success = false, message = "Số điện thoại xác minh không khớp với số điện thoại của đơn hàng này." });
            }
        }

        return Ok(new { success = true, order, isMasked = false });
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

    private static void MaskSensitiveCustomerInfo(OrderResponseDto order)
    {
        // 1. Che số điện thoại triệt để
        if (!string.IsNullOrWhiteSpace(order.PhoneNumber))
        {
            if (order.PhoneNumber.Length >= 7)
            {
                order.PhoneNumber = order.PhoneNumber[..3] + "****" + order.PhoneNumber[^3..];
            }
            else
            {
                order.PhoneNumber = "****";
            }
        }

        // 2. Bảo vệ địa chỉ nhận hàng
        if (!string.IsNullOrWhiteSpace(order.ShippingAddress))
        {
            var parts = order.ShippingAddress.Split(',');
            if (parts.Length > 2)
            {
                order.ShippingAddress = "***, " + string.Join(", ", parts.Skip(parts.Length - 2).Select(p => p.Trim()));
            }
            else
            {
                order.ShippingAddress = "*** (Đã bảo mật địa chỉ nhận hàng)";
            }
        }

        // 3. Che họ tên khách hàng (xử lý cả tên một từ)
        if (!string.IsNullOrWhiteSpace(order.CustomerName))
        {
            var name = order.CustomerName.Trim();
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                order.CustomerName = words[0][0] + "*** " + words[^1];
            }
            else
            {
                order.CustomerName = name.Length > 1 ? name[0] + "***" : "***";
            }
        }
    }

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
