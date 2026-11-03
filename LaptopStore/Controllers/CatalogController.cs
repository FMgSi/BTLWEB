using LaptopStore.Data;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using LaptopStore.ViewModels.Storefront;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers;

/// <summary>
/// Controller phục vụ khách hàng duyệt xem, tìm kiếm, lọc và xem chi tiết sản phẩm bằng Razor Views + Bootstrap
/// Route mặc định: /Catalog và /Store
/// </summary>
public class CatalogController : Controller
{
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;
    private readonly IDiscountService _discountService;
    private readonly AppDbContext _db;

    public CatalogController(IProductService productService, IOrderService orderService, IDiscountService discountService, AppDbContext db)
    {
        _productService = productService;
        _orderService = orderService;
        _discountService = discountService;
        _db = db;
    }

    /// <summary>
    /// GET: /Catalog hoặc /Store
    /// Trang danh mục sản phẩm dành cho khách hàng với đầy đủ bộ lọc, tìm kiếm và phân trang
    /// </summary>
    [HttpGet]
    [Route("Catalog")]
    [Route("Store")]
    public async Task<IActionResult> Index(
        string? q = null,
        string? brand = null,
        string? category = null,
        string? priceRange = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sort = "newest",
        int page = 1)
    {
        if (page < 1) page = 1;
        const int pageSize = 12;

        // Xử lý khoảng giá định sẵn nếu có
        if (!string.IsNullOrWhiteSpace(priceRange))
        {
            switch (priceRange)
            {
                case "under15":
                    maxPrice = 15000000m;
                    break;
                case "15to20":
                    minPrice = 15000000m;
                    maxPrice = 20000000m;
                    break;
                case "20to30":
                    minPrice = 20000000m;
                    maxPrice = 30000000m;
                    break;
                case "over30":
                    minPrice = 30000000m;
                    break;
            }
        }

        var pagedResult = await _productService.GetProductsApiAsync(
            q: q,
            tag: category,
            brands: brand,
            priceRange: priceRange,
            minPrice: minPrice,
            maxPrice: maxPrice,
            sortBy: sort ?? "newest",
            page: page,
            pageSize: pageSize);

        var availableBrands = await _db.Brands
            .AsNoTracking()
            .OrderBy(b => b.BrandName)
            .Select(b => b.BrandName)
            .ToListAsync();

        var availableCategories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .Select(c => c.CategoryName)
            .ToListAsync();

        var viewModel = new CatalogViewModel
        {
            Products = pagedResult,
            Search = q,
            Brand = brand,
            Category = category,
            PriceRange = priceRange,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            SortBy = sort ?? "newest",
            AvailableBrands = availableBrands,
            AvailableCategories = availableCategories
        };

        return View(viewModel);
    }

    /// <summary>
    /// GET: /Catalog/Details/{id}
    /// Trang chi tiết sản phẩm dành cho khách hàng
    /// </summary>
    [HttpGet]
    [Route("Catalog/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetProductDetailApiAsync(id);
        if (product == null)
        {
            return NotFound("Không tìm thấy sản phẩm yêu cầu.");
        }

        // Lấy danh sách sản phẩm liên quan cùng danh mục hoặc cùng thương hiệu
        var relatedProducts = await _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.ProductId != id && ((p.Brand != null && p.Brand.BrandName == product.Brand) || (p.Category != null && p.Category.CategoryName == product.Category)))
            .Take(4)
            .Select(p => new ProductApiDto
            {
                Id = p.ProductId,
                Name = p.ProductName,
                Brand = p.Brand != null ? p.Brand.BrandName : "",
                Category = p.Category != null ? p.Category.CategoryName : "",
                Price = p.DiscountPrice ?? p.Price,
                OriginalPrice = p.DiscountPrice.HasValue ? p.Price : null,
                Image = p.ThumbnailUrl ?? "/images/products/placeholder.png",
                Badge = p.StockQuantity > 0 ? null : "Hết hàng",
                BadgeColor = "bg-secondary"
            })
            .ToListAsync();

        ViewBag.RelatedProducts = relatedProducts;
        return View(product);
    }

    /// <summary>
    /// GET: /Catalog/Lookup
    /// Gỡ bỏ trang tra cứu cũ, chuyển hướng an toàn về trang danh mục và loại bỏ tham số nhạy cảm (orderCode, phone)
    /// </summary>
    [HttpGet]
    [Route("Catalog/Lookup")]
    public IActionResult Lookup()
    {
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// GET: /Promotions hoặc /Catalog/Promotions hoặc /khuyen-mai
    /// Gỡ bỏ chuyên mục khuyến mãi, chuyển hướng an toàn về danh sách sản phẩm mặc định
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
