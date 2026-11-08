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
        int pageSize = 5) // Giới hạn bảng hiển thị 5 sản phẩm mỗi trang theo yêu cầu
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
        var activeColors = await _db.Colors.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
        ViewBag.Colors = new SelectList(activeColors, "ColorId", "Name");

        return View(new ProductEditViewModel
        {
            Price = 20000000,
            StockQuantity = 10,
            SelectedColorId = activeColors.FirstOrDefault()?.ColorId,
            RamGB = 16,
            StorageGB = 512,
            StorageType = "SSD NVMe",
            RAM = "16GB",
            Storage = "512GB SSD NVMe",
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
            ViewBag.Colors = new SelectList(await _db.Colors.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(), "ColorId", "Name", vm.SelectedColorId);
            return View(vm);
        }

        var success = await _productService.CreateProductAsync(vm);
        if (!success)
        {
            ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu sản phẩm vào cơ sở dữ liệu. Vui lòng thử lại.");
            ViewBag.Brands = new SelectList(await _db.Brands.AsNoTracking().ToListAsync(), "BrandId", "BrandName", vm.BrandId);
            ViewBag.Categories = new SelectList(await _db.Categories.AsNoTracking().ToListAsync(), "CategoryId", "CategoryName", vm.CategoryId);
            ViewBag.Colors = new SelectList(await _db.Colors.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(), "ColorId", "Name", vm.SelectedColorId);
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
            RamGB = p.Specification?.RamGB ?? 16,
            StorageGB = p.Specification?.StorageGB ?? 512,
            StorageType = !string.IsNullOrWhiteSpace(p.Specification?.StorageType) ? p.Specification.StorageType : "SSD NVMe",
            RAM = p.Specification != null ? $"{p.Specification.RamGB}GB" : null,
            Storage = p.Specification != null ? $"{p.Specification.StorageGB}GB {p.Specification.StorageType}" : null,
            GPU = p.Specification?.GPU,
            ScreenSizeInch = p.Specification?.ScreenSizeInch,
            RefreshRateHz = p.Specification?.RefreshRateHz,
            WeightKg = p.Specification?.WeightKg,
            BatteryWh = p.Specification?.BatteryWh,
            OS = p.Specification?.OS,
            Slot1Url = p.Images.FirstOrDefault(i => i.DisplayOrder == 1)?.ImageUrl ?? p.ThumbnailUrl,
            Slot2Url = p.Images.FirstOrDefault(i => i.DisplayOrder == 2)?.ImageUrl,
            Slot3Url = p.Images.FirstOrDefault(i => i.DisplayOrder == 3)?.ImageUrl,
            Slot4Url = p.Images.FirstOrDefault(i => i.DisplayOrder == 4)?.ImageUrl
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
    /// Điều hướng thẳng sang trang Chỉnh sửa để thao tác trực tiếp, loại bỏ trang Xem trung gian
    /// </summary>
    public IActionResult Details(int? id)
    {
        if (id == null)
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Edit), new { id = id.Value });
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
    /// 7. Giao diện quản lý các phiên bản cấu hình & Màu sắc của một Laptop (GET: /Product/Variants/5)
    /// </summary>
    public async Task<IActionResult> Variants(int id)
    {
        var product = await _db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specification)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Color)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound();
        }

        ViewBag.ActiveColors = await _db.Colors
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(product);
    }

    /// <summary>
    /// 8. Thêm mới một phiên bản cấu hình & màu sắc cho Laptop (POST: /Product/AddVariant)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVariant(CreateProductVariantViewModel model)
    {
        var product = await _db.Products
            .Include(p => p.Specification)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.ProductId == model.ProductId);

        if (product == null)
        {
            return NotFound();
        }

        var color = await _db.Colors.FindAsync(model.ColorId);
        if (color == null)
        {
            TempData["ErrorMessage"] = "Màu sắc đã chọn không tồn tại.";
            return RedirectToAction(nameof(Variants), new { id = model.ProductId });
        }

        // Tự động sinh mã SKU nếu để trống
        string sku = string.IsNullOrWhiteSpace(model.SKU)
            ? $"SKU-{product.ProductId}-{color.Name.ToUpper().Replace(" ", "")}-{model.RamGB}-{model.StorageGB}"
            : model.SKU.Trim().ToUpper();

        // Kiểm tra trùng SKU
        if (await _db.ProductVariants.AnyAsync(v => v.SKU == sku))
        {
            TempData["ErrorMessage"] = $"Mã SKU '{sku}' đã tồn tại trên hệ thống. Vui lòng chọn mã SKU khác.";
            return RedirectToAction(nameof(Variants), new { id = model.ProductId });
        }

        // Kiểm tra trùng tổ hợp (ProductId, ColorId, RamGB, StorageGB)
        if (await _db.ProductVariants.AnyAsync(v => v.ProductId == model.ProductId && v.ColorId == model.ColorId && v.RamGB == model.RamGB && v.StorageGB == model.StorageGB))
        {
            TempData["ErrorMessage"] = $"Sản phẩm đã có phiên bản Màu '{color.Name}' với RAM {model.RamGB}GB và SSD {model.StorageGB}GB. Không thể thêm phiên bản trùng lặp.";
            return RedirectToAction(nameof(Variants), new { id = model.ProductId });
        }

        var variant = new ProductVariant
        {
            ProductId = model.ProductId,
            ColorId = model.ColorId,
            RamGB = model.RamGB,
            StorageGB = model.StorageGB,
            SKU = sku,
            StockQuantity = Math.Max(0, model.StockQuantity),
            Price = model.Price > 0 ? model.Price : null,
            DiscountPrice = model.DiscountPrice,
            ImageUrl = (!string.IsNullOrWhiteSpace(model.ImageUrl) && model.ImageUrl.Trim() != product.ThumbnailUrl)
                ? model.ImageUrl.Trim()
                : null,
            IsActive = true
        };

        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync();

        // Đồng bộ lại tổng tồn kho Product = tổng tồn kho các variant
        var totalStock = await _db.ProductVariants.Where(v => v.ProductId == model.ProductId).SumAsync(v => v.StockQuantity);
        product.StockQuantity = totalStock;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã thêm thành công phiên bản Màu {color.Name} (RAM {variant.RamGB}GB / SSD {variant.StorageGB}GB, tồn kho: {variant.StockQuantity})!";
        return RedirectToAction(nameof(Variants), new { id = model.ProductId });
    }

    /// <summary>
    /// 9. Điều chỉnh số lượng tồn kho của một phiên bản cụ thể (POST: /Product/AdjustVariantStock)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustVariantStock(AdjustVariantStockViewModel model)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Color)
            .FirstOrDefaultAsync(v => v.VariantId == model.VariantId);

        if (variant == null)
        {
            return NotFound();
        }

        if (model.NewStockQuantity < 0)
        {
            TempData["ErrorMessage"] = "Số lượng tồn kho không được âm.";
            return RedirectToAction(nameof(Variants), new { id = variant.ProductId });
        }

        variant.StockQuantity = model.NewStockQuantity;
        await _db.SaveChangesAsync();

        // Đồng bộ tổng tồn kho trên Product
        var totalStock = await _db.ProductVariants.Where(v => v.ProductId == variant.ProductId).SumAsync(v => v.StockQuantity);
        var product = await _db.Products.FindAsync(variant.ProductId);
        if (product != null)
        {
            product.StockQuantity = totalStock;
            await _db.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = $"Đã cập nhật tồn kho phiên bản '{variant.SKU}' ({variant.Color?.Name}) thành {model.NewStockQuantity} máy!";
        return RedirectToAction(nameof(Variants), new { id = variant.ProductId });
    }

    /// <summary>
    /// Cập nhật ảnh riêng cho phiên bản cụ thể (POST: /Product/UpdateVariantImage)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVariantImage(UpdateVariantImageViewModel model)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Color)
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.VariantId == model.VariantId);

        if (variant == null)
        {
            return NotFound();
        }

        var trimmedUrl = model.ImageUrl?.Trim();
        // Nếu để trống hoặc trùng với ảnh đại diện chính của mẫu máy thì lưu null để kế thừa
        variant.ImageUrl = (!string.IsNullOrWhiteSpace(trimmedUrl) && trimmedUrl != variant.Product?.ThumbnailUrl)
            ? trimmedUrl
            : null;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã cập nhật ảnh cho phiên bản SKU '{variant.SKU}' ({variant.Color?.Name}) thành công!";
        return RedirectToAction(nameof(Variants), new { id = variant.ProductId });
    }

    /// <summary>
    /// 10. Chuyển trạng thái kích hoạt phiên bản (POST: /Product/ToggleVariant/5)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVariant(int id)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Color)
            .FirstOrDefaultAsync(v => v.VariantId == id);

        if (variant == null)
        {
            return NotFound();
        }

        variant.IsActive = !variant.IsActive;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã {(variant.IsActive ? "kích hoạt" : "vô hiệu hóa")} phiên bản {variant.SKU} ({variant.Color?.Name})!";
        return RedirectToAction(nameof(Variants), new { id = variant.ProductId });
    }

    /// <summary>
    /// 11. Xóa một phiên bản cấu hình & màu sắc (POST: /Product/DeleteVariant/5)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(int id)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Color)
            .FirstOrDefaultAsync(v => v.VariantId == id);

        if (variant == null)
        {
            return NotFound();
        }

        int productId = variant.ProductId;
        string sku = variant.SKU;
        string colorName = variant.Color?.Name ?? "Không xác định";

        // Tách các liên kết với OrderItems (nếu có) để bảo toàn lịch sử đơn hàng
        var orderItems = await _db.OrderItems.Where(oi => oi.VariantId == id).ToListAsync();
        foreach (var oi in orderItems)
        {
            oi.VariantId = null;
        }

        _db.ProductVariants.Remove(variant);
        await _db.SaveChangesAsync();

        // Đồng bộ lại tổng tồn kho của sản phẩm gốc
        var product = await _db.Products.FindAsync(productId);
        if (product != null)
        {
            var totalStock = await _db.ProductVariants
                .Where(v => v.ProductId == productId)
                .SumAsync(v => v.StockQuantity);
            product.StockQuantity = totalStock;
            await _db.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = $"Đã xóa thành công phiên bản '{sku}' ({colorName})!";
        return RedirectToAction(nameof(Variants), new { id = productId });
    }

    /// <summary>
    /// Chuyển hướng nhanh sang trang Quản lý sản phẩm
    /// </summary>
    public IActionResult Dashboard() => RedirectToAction("Index");
}