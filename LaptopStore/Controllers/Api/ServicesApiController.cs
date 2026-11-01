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

    /// <summary>
    /// GET /api/services/catalog?type=repair | warranty
    /// Lấy danh mục dịch vụ Sửa chữa & Bảo hành từ CSDL
    /// </summary>
    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog([FromQuery] string? type)
    {
        var items = await _warrantyService.GetCatalogAsync(type);
        return Ok(new { success = true, items });
    }

    /// <summary>
    /// GET /api/services/my-devices?phoneNumber=0987654321
    /// Lấy danh sách máy khách hàng đã mua và đã nhận để chọn bảo hành
    /// </summary>
    [HttpGet("my-devices")]
    public async Task<IActionResult> GetMyDevices([FromQuery] string? phoneNumber)
    {
        int? userId = null;
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
            // Bỏ qua nếu Session chưa cấu hình
        }

        if (!userId.HasValue && string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập số điện thoại hoặc đăng nhập để tải danh sách máy đã mua." });
        }

        var devices = await _warrantyService.GetCustomerDevicesAsync(phoneNumber, userId);
        return Ok(new { success = true, devices });
    }

    /// <summary>
    /// GET /api/services/device-by-serial/{serial}
    /// Tra cứu chi tiết một máy theo số Serial
    /// </summary>
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

    /// <summary>
    /// POST /api/services/warranty-ticket
    /// Tiếp nhận yêu cầu bảo hành cho máy đã mua từ cửa hàng
    /// </summary>
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

    /// <summary>
    /// POST /api/services/repair-ticket
    /// Tiếp nhận yêu cầu sửa chữa cho máy đã mua hoặc máy mua ngoài
    /// </summary>
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

    /// <summary>
    /// GET /api/services/ticket/{code}?phone=0987654321
    /// Tra cứu chi tiết phiếu và timeline xử lý theo mã phiếu và số điện thoại
    /// </summary>
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

    /// <summary>
    /// POST /api/services/quote-decision
    /// Khách hàng duyệt hoặc từ chối báo giá sửa chữa
    /// </summary>
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
