using LaptopStore.Data;
using LaptopStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace LaptopStore.Controllers;

/// <summary>
/// Controller xử lý chuyển tiếp an toàn các liên kết Catalog MVC cũ sang React Frontend
/// Tuyến đường: /Catalog, /Store, /Catalog/Details/{id}, /Catalog/Lookup, /Promotions
/// </summary>
public class CatalogController : Controller
{
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;
    private readonly IDiscountService _discountService;
    private readonly AppDbContext _db;
    private readonly IConfiguration? _configuration;

    private string FrontendBaseUrl => _configuration?["FrontendUrl"] ?? "http://localhost:3000";

    public CatalogController(
        IProductService? productService = null,
        IOrderService? orderService = null,
        IDiscountService? discountService = null,
        AppDbContext? db = null,
        IConfiguration? configuration = null)
    {
        _productService = productService!;
        _orderService = orderService!;
        _discountService = discountService!;
        _db = db!;
        _configuration = configuration;
    }

    /// <summary>
    /// GET: /Catalog hoặc /Store
    /// Chuyển tiếp người dùng sang trang danh mục sản phẩm React kèm đầy đủ tham số bộ lọc nếu có
    /// </summary>
    [HttpGet]
    [Route("Catalog")]
    [Route("Store")]
    public IActionResult Index(
        string? q = null,
        string? brand = null,
        string? category = null,
        string? priceRange = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sort = "newest",
        int page = 1)
    {
        string queryParams = Request?.QueryString.HasValue == true ? Request.QueryString.Value! : "";
        return Redirect($"{FrontendBaseUrl}/products{queryParams}");
    }

    /// <summary>
    /// GET: /Catalog/Details/{id}
    /// Chuyển tiếp người dùng sang trang chi tiết sản phẩm trên React Frontend
    /// </summary>
    [HttpGet]
    [Route("Catalog/Details/{id:int}")]
    public IActionResult Details(int id)
    {
        return Redirect($"{FrontendBaseUrl}/product/{id}");
    }

    /// <summary>
    /// GET: /Catalog/Lookup
    /// Gỡ bỏ trang tra cứu cũ, chuyển hướng an toàn về trang Tài khoản cá nhân trên React Frontend
    /// </summary>
    [HttpGet]
    [Route("Catalog/Lookup")]
    public IActionResult Lookup()
    {
        return Redirect($"{FrontendBaseUrl}/account");
    }

    /// <summary>
    /// GET: /Promotions hoặc /Catalog/Promotions hoặc /khuyen-mai
    /// Gỡ bỏ chuyên mục khuyến mãi, chuyển hướng an toàn về danh mục sản phẩm mặc định
    /// </summary>
    [HttpGet]
    [Route("Promotions")]
    [Route("Catalog/Promotions")]
    [Route("khuyen-mai")]
    public IActionResult Promotions()
    {
        return RedirectToAction(nameof(Index));
    }
}
