using LaptopStore.Data;
using LaptopStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace LaptopStore.Controllers;

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

    [HttpGet]
    [Route("Catalog/Details/{id:int}")]
    public IActionResult Details(int id)
    {
        return Redirect($"{FrontendBaseUrl}/product/{id}");
    }

    [HttpGet]
    [Route("Catalog/Lookup")]
    public IActionResult Lookup()
    {
        return Redirect($"{FrontendBaseUrl}/account");
    }

    [HttpGet]
    [Route("Promotions")]
    [Route("Catalog/Promotions")]
    [Route("khuyen-mai")]
    public IActionResult Promotions()
    {
        return RedirectToAction(nameof(Index));
    }
}
