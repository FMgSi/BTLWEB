using System.Threading.Tasks;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers.Api;

[ApiController]
[Route("api/promotions")]
public class PromotionsApiController : ControllerBase
{
    private readonly IDiscountService _discountService;

    public PromotionsApiController(IDiscountService discountService)
    {
        _discountService = discountService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActivePromotions()
    {
        var promotions = await _discountService.GetActivePromotionsForStorefrontAsync();
        return Ok(new { success = true, promotions });
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateDiscount([FromBody] ValidateDiscountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập mã giảm giá cần áp dụng." });
        }

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
