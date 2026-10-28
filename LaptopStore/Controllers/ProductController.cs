using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers;

/// <summary>
/// Controller Quản trị (Admin) dành cho việc quản lý sản phẩm Laptop
/// Được bảo vệ chặt chẽ bởi Cookie Authentication: [Authorize(Roles = "Admin")]
/// </summary>
[Authorize(Roles = "Admin")]
public class ProductController : Controller
{
    private readonly AppDbContext _db;
    private readonly IProductService _productService;
    private readonly IWebHostEnvironment _env;

    public ProductController(AppDbContext db, IProductService productService, IWebHostEnvironment env)
    {
        _db = db;
        _productService = productService;
        _env = env;
    }

    /// <summary>
    /// 1. Hiển thị danh sách sản phẩm cho Admin (Tìm kiếm từ khóa, Lọc Hãng, Danh mục, Trạng thái kho, Khoảng giá, Phân trang)
    /// GET: /Product
    /// </summary>
    public async Task<IActionResult> Index(
        string? keyword,
        int? brandId,
        int? categoryId,
        string? status,
        string? priceRange,
        int page = 1,
        int pageSize = 10)
    {
        // Ghi nhận lượt truy cập qua Cookie
        var visits = int.Parse(Request.Cookies["AdminVisits"] ?? "0") + 1;
        Response.Cookies.Append("AdminVisits", visits.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });
        ViewBag.Visits = visits;

        var (list, total, brands, categories) = await _productService.GetAdminProductsAsync(
            keyword, brandId, categoryId, status, priceRange, page, pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        if (ViewBag.TotalPages == 0) ViewBag.TotalPages = 1;

        ViewBag.Keyword = keyword;
        ViewBag.BrandId = brandId;
        ViewBag.CategoryId = categoryId;
        ViewBag.Status = status ?? "all";
        ViewBag.PriceRange = priceRange ?? "all";

        ViewBag.Brands = new SelectList(brands, "BrandId", "BrandName", brandId);
        ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", categoryId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_ProductTablePartial", list);
        }

        return View(list);
    }

    /// <summary>
    /// 2. Giao diện Thêm mới sản phẩm (GET)
    /// GET: /Product/Create
    /// </summary>
    public async Task<IActionResult> Create()
    {
        ViewBag.Brands = new SelectList(await _db.Brands.AsNoTracking().ToListAsync(), "BrandId", "BrandName");
        ViewBag.Categories = new SelectList(await _db.Categories.AsNoTracking().ToListAsync(), "CategoryId", "CategoryName");
        return View(new ProductEditViewModel
        {
            Price = 20000000,
            StockQuantity = 10,
            RAM = "16GB",
            Storage = "512GB SSD",
            ScreenSizeInch = 15.6m,
            RefreshRateHz = 144,
            WeightKg = 1.9m,
            BatteryWh = 60,
            OS = "Windows 11 Home"
        });
    }

    /// <summary>
    /// 2. Xử lý lưu sản phẩm mới vào Database (POST)
    /// POST: /Product/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Brands = new SelectList(await _db.Brands.AsNoTracking().ToListAsync(), "BrandId", "BrandName", vm.BrandId);
            ViewBag.Categories = new SelectList(await _db.Categories.AsNoTracking().ToListAsync(), "CategoryId", "CategoryName", vm.CategoryId);
            return View(vm);
        }

        var success = await _productService.CreateProductAsync(vm);
        if (!success)
        {
            ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu sản phẩm vào cơ sở dữ liệu. Vui lòng thử lại.");
            ViewBag.Brands = new SelectList(await _db.Brands.AsNoTracking().ToListAsync(), "BrandId", "BrandName", vm.BrandId);
            ViewBag.Categories = new SelectList(await _db.Categories.AsNoTracking().ToListAsync(), "CategoryId", "CategoryName", vm.CategoryId);
            return View(vm);
        }

        TempData["Success"] = $"Đã thêm thành công sản phẩm: {vm.ProductName}";
        TempData["SuccessMessage"] = $"Thêm mới laptop {vm.ProductName} thành công!";
        HttpContext.Session.SetString("LastAction", $"Vừa thêm thành công sản phẩm: {vm.ProductName}");

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 3. Giao diện Chỉnh sửa sản phẩm (GET)
    /// GET: /Product/Edit/{id}
    /// </summary>
    public async Task<IActionResult> Edit(int id)
    {
        var p = await _productService.GetProductByIdAsync(id);
        if (p == null) return NotFound();

        var vm = new ProductEditViewModel
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            BrandId = p.BrandId,
            CategoryId = p.CategoryId,
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            StockQuantity = p.StockQuantity,
            Description = p.Description,
            ExistingThumbnail = p.ThumbnailUrl,
            CPU = p.Specification?.CPU,
            RAM = p.Specification != null ? $"{p.Specification.RamGB}GB" : null,
            Storage = p.Specification != null ? $"{p.Specification.StorageGB}GB {p.Specification.StorageType}" : null,
            GPU = p.Specification?.GPU,
            ScreenSizeInch = p.Specification?.ScreenSizeInch,
            RefreshRateHz = p.Specification?.RefreshRateHz,
            WeightKg = p.Specification?.WeightKg,
            BatteryWh = p.Specification?.BatteryWh,
            OS = p.Specification?.OS
        };

        ViewBag.Brands = new SelectList(await _db.Brands.AsNoTracking().ToListAsync(), "BrandId", "BrandName", p.BrandId);
        ViewBag.Categories = new SelectList(await _db.Categories.AsNoTracking().ToListAsync(), "CategoryId", "CategoryName", p.CategoryId);
        return View(vm);
    }

    /// <summary>
    /// 3. Xử lý cập nhật thông tin sản phẩm vào Database (POST)
    /// POST: /Product/Edit/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Brands = new SelectList(await _db.Brands.AsNoTracking().ToListAsync(), "BrandId", "BrandName", vm.BrandId);
            ViewBag.Categories = new SelectList(await _db.Categories.AsNoTracking().ToListAsync(), "CategoryId", "CategoryName", vm.CategoryId);
            return View(vm);
        }

        var success = await _productService.UpdateProductAsync(vm);
        if (!success)
        {
            ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật sản phẩm. Vui lòng thử lại.");
            ViewBag.Brands = new SelectList(await _db.Brands.AsNoTracking().ToListAsync(), "BrandId", "BrandName", vm.BrandId);
            ViewBag.Categories = new SelectList(await _db.Categories.AsNoTracking().ToListAsync(), "CategoryId", "CategoryName", vm.CategoryId);
            return View(vm);
        }

        TempData["Success"] = $"Cập nhật thành công: {vm.ProductName}";
        TempData["SuccessMessage"] = $"Cập nhật thông tin laptop {vm.ProductName} thành công!";
        HttpContext.Session.SetString("LastAction", $"Cập nhật thành công: {vm.ProductName}");

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 4. Xem chi tiết thông tin sản phẩm laptop (GET: /Product/Details/5)
    /// Triển khai theo chuẩn mô hình được học trong LapTrinhWeb (TvcLesson08Df)
    /// </summary>
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specification)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(m => m.ProductId == id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    /// <summary>
    /// 5. Giao diện Xác nhận xóa sản phẩm (GET: /Product/Delete/5)
    /// Triển khai theo chuẩn mô hình được học trong LapTrinhWeb (TvcLesson08Df)
    /// </summary>
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specification)
            .FirstOrDefaultAsync(m => m.ProductId == id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    /// <summary>
    /// 6. Xử lý xóa sản phẩm (POST: /Product/Delete/5)
    /// Hỗ trợ cả Form submit truyền thống (theo giáo trình MVC) lẫn AJAX Fetch từ giao diện
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _db.Products.FindAsync(id);
        var productName = product?.ProductName ?? $"#{id}";
        var success = await _productService.DeleteProductAsync(id);

        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                     (Request.ContentType != null && Request.ContentType.Contains("application/json"));

        if (isAjax)
        {
            if (success)
            {
                return Json(new { success = true, message = $"Xóa sản phẩm {productName} thành công" });
            }
            return Json(new { success = false, message = "Không tìm thấy sản phẩm hoặc sản phẩm đã có đơn hàng liên kết" });
        }

        if (success)
        {
            TempData["SuccessMessage"] = $"Đã xóa sản phẩm {productName} thành công!";
            TempData["Success"] = $"Đã xóa sản phẩm {productName} thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể xóa sản phẩm vì sản phẩm đang có trong đơn hàng của khách!";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Chuyển hướng nhanh sang trang Quản lý sản phẩm
    /// </summary>
    public IActionResult Dashboard() => RedirectToAction("Index");
}