using System;
using System.Linq;
using System.Threading.Tasks;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers;

[Route("account")]
[Route("tai-khoan")]
public class AccountController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IWarrantyService _warrantyService;

    public AccountController(IOrderService orderService, IWarrantyService warrantyService)
    {
        _orderService = orderService;
        _warrantyService = warrantyService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? tab = "orders", string? code = null, string? phone = null)
    {
        var model = new AccountStorefrontViewModel
        {
            ActiveTab = string.IsNullOrWhiteSpace(tab) ? "orders" : tab.ToLower(),
            GuestOrderCode = code,
            GuestPhone = phone
        };

        int? currentUserId = null;
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
        {
            currentUserId = uid;
        }

        if (currentUserId.HasValue)
        {
            model.IsAuthenticated = true;
            model.CustomerName = HttpContext.Session.GetString("Username") ?? User.Identity?.Name ?? "Khách hàng";
            model.Orders = await _orderService.GetUserOrdersAsync(currentUserId.Value);
            model.Devices = await _warrantyService.GetCustomerDevicesAsync(null, currentUserId.Value);

            var completedOrders = model.Orders
                .Where(o => o.Status == "Hoàn thành" || o.Status == "Đã giao")
                .ToList();

            model.TotalSpent = completedOrders.Sum(o => o.TotalAmount);

            if (model.TotalSpent >= 100000000m)
            {
                model.MemberTier = "Kim Cương";
                model.TierColor = "#0284c7";
                model.NextTier = "Tối Đa";
                model.RemainingToNextTier = 0;
                model.ProgressPercent = 100;
            }
            else if (model.TotalSpent >= 50000000m)
            {
                model.MemberTier = "Vàng";
                model.TierColor = "#eab308";
                model.NextTier = "Kim Cương";
                model.RemainingToNextTier = 100000000m - model.TotalSpent;
                model.ProgressPercent = (int)Math.Min(100, Math.Round(((model.TotalSpent - 50000000m) / 50000000m) * 100));
            }
            else if (model.TotalSpent >= 20000000m)
            {
                model.MemberTier = "Bạc";
                model.TierColor = "#94a3b8";
                model.NextTier = "Vàng";
                model.RemainingToNextTier = 50000000m - model.TotalSpent;
                model.ProgressPercent = (int)Math.Min(100, Math.Round(((model.TotalSpent - 20000000m) / 30000000m) * 100));
            }
            else
            {
                model.MemberTier = "Đồng";
                model.TierColor = "#a87552";
                model.NextTier = "Bạc";
                model.RemainingToNextTier = 20000000m - model.TotalSpent;
                model.ProgressPercent = (int)Math.Min(100, Math.Round((model.TotalSpent / 20000000m) * 100));
            }
        }
        else if (!string.IsNullOrWhiteSpace(code))
        {
            var order = await _orderService.GetOrderByCodeAsync(code);
            if (order != null)
            {
                if (string.IsNullOrWhiteSpace(phone) || (order.PhoneNumber?.Trim() != phone.Trim()))
                {
                    model.GuestLookupError = "Đã tìm thấy đơn hàng nhưng số điện thoại xác nhận không khớp.";
                }
                else
                {
                    model.GuestOrderResult = order;
                }
            }
            else
            {
                model.GuestLookupError = $"Không tìm thấy đơn hàng với mã #{code}.";
            }
        }

        return View(model);
    }
}
