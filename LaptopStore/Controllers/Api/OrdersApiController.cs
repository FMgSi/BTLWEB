using System;
using System.Linq;
using System.Threading.Tasks;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers.Api;

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

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join("; ", errors) });
        }

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

        bool isAdmin = User.IsInRole("Admin") || HttpContext.Session.GetString("Role") == "Admin";

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

        if (!isOwner)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                
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

    [HttpDelete("{id:int}")]
    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteCustomerOrder(int id)
    {
        int? userId = null;
        try
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
            {
                userId = uid;
            }
        }
        catch { }

        var (success, message) = await _orderService.CancelOrDeleteOrderAsync(id, userId);
        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message });
    }

    [HttpDelete("code/{orderCode}")]
    [HttpPost("code/{orderCode}/delete")]
    public async Task<IActionResult> DeleteCustomerOrderByCode(string orderCode, [FromQuery] string? phone = null)
    {
        var order = await _orderService.GetOrderByCodeAsync(orderCode);
        if (order == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy đơn hàng: " + orderCode });
        }

        int? userId = null;
        try
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
            {
                userId = uid;
            }
        }
        catch { }

        var (success, message) = await _orderService.CancelOrDeleteOrderAsync(order.OrderId, userId, phone);
        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message });
    }

    [HttpPost("pay-installment-period")]
    public async Task<IActionResult> PayInstallmentPeriod([FromBody] PayInstallmentPeriodRequest request)
    {
        if (request == null || request.ContractId <= 0 || request.PeriodNumber <= 0)
        {
            return BadRequest(new { success = false, message = "Thông tin thanh toán kỳ trả góp không hợp lệ." });
        }

        string customerName = HttpContext.Session.GetString("FullName") ?? "Khách hàng";
        string method = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "VietQR" : request.PaymentMethod;
        string txRef = "ONLINE-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        string note = request.Note ?? $"Khách hàng {customerName} thanh toán kỳ {request.PeriodNumber} trực tuyến qua {method}";

        var (success, message, schedule) = await _installmentService.CollectPeriodPaymentAsync(
            request.ContractId, request.PeriodNumber, customerName, method, txRef, note);

        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message, schedule });
    }

    [HttpPost("pay-installment-prepay")]
    public async Task<IActionResult> PayInstallmentPrepay([FromBody] PayInstallmentPrepayRequest request)
    {
        if (request == null || request.ContractId <= 0)
        {
            return BadRequest(new { success = false, message = "Thông tin hợp đồng không hợp lệ." });
        }

        string customerName = HttpContext.Session.GetString("FullName") ?? "Khách hàng";
        string method = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "VietQR" : request.PaymentMethod;
        string txRef = "PREPAY-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        string note = request.Note ?? $"Khách hàng {customerName} thanh toán khoản trả trước 30% trực tuyến qua {method}";

        var (success, message, contract) = await _installmentService.ActivateContractAsync(
            request.ContractId, customerName, method, txRef, note);

        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message, contract });
    }

    private static void MaskSensitiveCustomerInfo(OrderResponseDto order)
    {
        
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
