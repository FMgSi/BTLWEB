using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers.Api;

[ApiController]
[Route("api/services")]
public class ServicesApiController : ControllerBase
{
    private readonly IWarrantyService _warrantyService;

    public ServicesApiController(IWarrantyService warrantyService)
    {
        _warrantyService = warrantyService;
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog([FromQuery] string? type)
    {
        var items = await _warrantyService.GetCatalogAsync(type);
        return Ok(new { success = true, items });
    }

    [HttpGet("my-devices")]
    public async Task<IActionResult> GetMyDevices([FromQuery] string? phoneNumber, [FromQuery] string? orderCode, [FromQuery] int? userId)
    {
        if (!userId.HasValue || userId.Value <= 0)
        {
            try
            {
                var userIdStr = HttpContext.Session?.GetString("UserId");
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }
            }
            catch
            {
                
            }
        }

        var devices = await _warrantyService.GetCustomerDevicesAsync(phoneNumber, userId, orderCode);
        return Ok(new { success = true, devices });
    }

    [HttpGet("my-tickets")]
    public async Task<IActionResult> GetMyTickets([FromQuery] string? phoneNumber, [FromQuery] int? userId)
    {
        if (!userId.HasValue || userId.Value <= 0)
        {
            try
            {
                var userIdStr = HttpContext.Session?.GetString("UserId");
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }
            }
            catch { }
        }

        var tickets = await _warrantyService.GetCustomerTicketsAsync(phoneNumber, userId);
        return Ok(new { success = true, tickets });
    }

    [HttpGet("device-by-serial/{serial}")]
    public async Task<IActionResult> GetDeviceBySerial(string serial)
    {
        var device = await _warrantyService.GetDeviceBySerialAsync(serial);
        if (device == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy thiết bị với số Serial: {serial}" });
        }
        return Ok(new { success = true, device });
    }

    [HttpPost("warranty-ticket")]
    public async Task<IActionResult> CreateWarrantyTicket([FromBody] CreateWarrantyTicketRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join("; ", errors) });
        }

        int? userId = null;
        try
        {
            var userIdStr = HttpContext.Session?.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
            {
                userId = parsedId;
            }
        }
        catch { }

        var result = await _warrantyService.CreateWarrantyTicketAsync(request, userId);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, ticket = result.Ticket });
    }

    [HttpPost("repair-ticket")]
    public async Task<IActionResult> CreateRepairTicket([FromBody] CreateRepairTicketRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join("; ", errors) });
        }

        int? userId = null;
        try
        {
            var userIdStr = HttpContext.Session?.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
            {
                userId = parsedId;
            }
        }
        catch { }

        var result = await _warrantyService.CreateRepairTicketAsync(request, userId);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, ticket = result.Ticket });
    }

    [HttpGet("ticket/{code}")]
    public async Task<IActionResult> GetTicket(string code, [FromQuery] string? phone)
    {
        var ticket = await _warrantyService.GetTicketByCodeAsync(code, phone);
        if (ticket == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phiếu hoặc số điện thoại xác thực không chính xác." });
        }

        return Ok(new { success = true, ticket });
    }

    [HttpPost("quote-decision")]
    public async Task<IActionResult> SubmitQuoteDecision([FromBody] CustomerDecisionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
        }

        var result = await _warrantyService.SubmitQuoteDecisionAsync(0, request.QuoteId, request.Decision, request.Notes, request.PhoneNumber);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }
}
