using LaptopStore.Data;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers.Api;

/// <summary>
/// Controller API cung cấp dữ liệu sản phẩm cho giao diện React Frontend
/// Đường dẫn gốc: /api/products
/// Sử dụng kiến trúc Clean MVC: Ủy nhiệm toàn bộ nghiệp vụ lọc, tính giá, chi tiết sang IProductService
/// </summary>
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

    /// <summary>
    /// Constructor dự phòng hỗ trợ Unit Testing trực tiếp với DbContext
    /// </summary>
    public ProductsApiController(AppDbContext db)
    {
        _productService = new ProductService(db, null!);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm có hỗ trợ bộ lọc và phân trang
    /// GET: /api/products?tag=...&brands=...&cpus=...&page=1
    /// </summary>
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

    /// <summary>
    /// Lấy chi tiết một sản phẩm theo ID (Tự động lọc trạng thái IsActive)
    /// GET: /api/products/{id}
    /// </summary>
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
}
