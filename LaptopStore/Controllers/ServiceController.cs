using System;
using System.Threading.Tasks;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers;

[Route("service")]
[Route("Service")]
[Route("bao-hanh")]
[Route("sua-chua")]
public class ServiceController : Controller
{
    private readonly IWarrantyService _warrantyService;

    public ServiceController(IWarrantyService warrantyService)
    {
        _warrantyService = warrantyService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? tab = "request",
        string? code = null,
        string? phone = null,
        int? deviceId = null)
    {
        var model = new ServiceStorefrontViewModel
        {
            ActiveTab = string.IsNullOrWhiteSpace(tab) ? "request" : tab.ToLower(),
            LookupCode = code,
            LookupPhone = phone,
            PreSelectedDeviceId = deviceId
        };

        // 1. Tải bảng giá dịch vụ
        model.Catalog = await _warrantyService.GetCatalogAsync();

        // 2. Xác định UserId hoặc SĐT nếu đã đăng nhập
        int? currentUserId = null;
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
        {
            currentUserId = uid;
        }

        // 3. Tải danh sách thiết bị đã mua
        if (currentUserId.HasValue || !string.IsNullOrWhiteSpace(phone))
        {
            model.MyDevices = await _warrantyService.GetCustomerDevicesAsync(phone, currentUserId);
        }

        // 4. Nếu có mã tra cứu
        if (!string.IsNullOrWhiteSpace(code))
        {
            model.ActiveTab = "lookup";
            model.LookupResult = await _warrantyService.GetTicketByCodeAsync(code, phone);
            if (model.LookupResult == null)
            {
                model.LookupError = "Không tìm thấy thông tin phiếu dịch vụ hoặc số điện thoại xác thực chưa khớp.";
            }
        }

        return View(model);
    }

    [HttpPost("submit-warranty")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitWarranty(CreateWarrantyTicketRequest request)
    {
        int? currentUserId = null;
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
        {
            currentUserId = uid;
        }

        var result = await _warrantyService.CreateWarrantyTicketAsync(request, currentUserId);
        if (!result.Success || result.Ticket == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { tab = "request", deviceId = request.SoldDeviceId });
        }

        TempData["SuccessMessage"] = $"Gửi yêu cầu bảo hành thành công! Mã phiếu: #{result.Ticket.TicketCode}";
        return RedirectToAction(nameof(Index), new { tab = "lookup", code = result.Ticket.TicketCode, phone = request.PhoneNumber });
    }

    [HttpPost("submit-repair")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitRepair(CreateRepairTicketRequest request)
    {
        int? currentUserId = null;
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
        {
            currentUserId = uid;
        }

        var result = await _warrantyService.CreateRepairTicketAsync(request, currentUserId);
        if (!result.Success || result.Ticket == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { tab = "request" });
        }

        TempData["SuccessMessage"] = $"Gửi yêu cầu sửa chữa thành công! Mã phiếu: #{result.Ticket.TicketCode}";
        return RedirectToAction(nameof(Index), new { tab = "lookup", code = result.Ticket.TicketCode, phone = request.PhoneNumber });
    }

    [HttpPost("quote-decision")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQuoteDecision(int quoteId, string decision, string? notes, string? phone, string? ticketCode)
    {
        var result = await _warrantyService.SubmitQuoteDecisionAsync(0, quoteId, decision, notes, phone);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"Đã ghi nhận quyết định '{decision}' của quý khách thành công!";
        }

        return RedirectToAction(nameof(Index), new { tab = "lookup", code = ticketCode, phone });
    }
}

