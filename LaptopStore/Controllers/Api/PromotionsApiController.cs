using System.Threading.Tasks;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers.Api;

/// <summary>
/// Web API Khuyến mãi & Voucher dành cho khách hàng trên website (React Frontend)
/// Đường dẫn chuẩn: /api/promotions
/// </summary>
[ApiController]
[Route("api/promotions")]
public class PromotionsApiController : ControllerBase
{
    private readonly IDiscountService _discountService;

    public PromotionsApiController(IDiscountService discountService)
    {
        _discountService = discountService;
    }

    /// <summary>
    /// GET /api/promotions
    /// Lấy danh sách các voucher và ưu đãi đang có hiệu lực để khách hàng lưu vào ví voucher
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActivePromotions()
    {
        var promotions = await _discountService.GetActivePromotionsForStorefrontAsync();
        return Ok(new { success = true, promotions });
    }

    /// <summary>
    /// POST /api/promotions/validate
    /// Kiểm tra tính hợp lệ của mã giảm giá với các sản phẩm đang có trong giỏ hàng
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateDiscount([FromBody] ValidateDiscountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập mã giảm giá cần áp dụng." });
        }

        // Lấy UserId nếu khách đã đăng nhập
        int? userId = null;
        try
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
            {
                userId = parsedId;
            }
        }
        catch { }

        var result = await _discountService.ValidateDiscountForCartAsync(
            request.Code, request.SubTotal, request.Items, request.PhoneNumber, userId);

        if (!result.IsValid)
        {
            return Ok(new
            {
                success = false,
                message = result.Message,
                needMoreAmount = result.NeedMoreAmount,
                minOrderAmount = result.MinOrderAmount
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            code = result.Code,
            title = result.Title,
            discountType = result.DiscountType,
            discountAmount = result.DiscountAmount,
            subTotal = result.SubTotal,
            totalPayable = result.TotalPayable
        });
    }
}

