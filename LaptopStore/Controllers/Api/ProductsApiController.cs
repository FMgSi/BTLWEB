using LaptopStore.Data;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers.Api;

[ApiController]
[Route("api/products")]
public class ProductsApiController : ControllerBase
{
    private readonly IProductService _productService;

    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public ProductsApiController(IProductService productService)
    {
        _productService = productService;
    }

    public ProductsApiController(AppDbContext db)
    {
        _productService = new ProductService(db, null!);
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? q = null,
        [FromQuery] string? tag = null,
        [FromQuery] string? brands = null,
        [FromQuery] string? sizes = null,
        [FromQuery] string? cpus = null,
        [FromQuery] string? rams = null,
        [FromQuery] string? priceRange = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var result = await _productService.GetProductsApiAsync(
            q, tag, brands, sizes, cpus, rams, priceRange, minPrice, maxPrice, sortBy, page, pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var detail = await _productService.GetProductDetailApiAsync(id);
        if (detail == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy sản phẩm hoặc sản phẩm đã ngừng kinh doanh." });
        }

        return Ok(detail);
    }

    [HttpGet("suggest-models")]
    public async Task<IActionResult> SuggestModels([FromQuery] string? q = null)
    {
        var suggestions = await _productService.SuggestModelsAsync(q);
        return Ok(suggestions);
    }
}
