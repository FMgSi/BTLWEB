using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers;

public class ProductController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // 1. Quản lý danh sách + AJAX Search & Paging
    public async Task<IActionResult> Index(string? keyword, int? categoryId, int page = 1)
    {
        // Ghi nhận cookie số lần xem trang admin
        var visits = int.Parse(Request.Cookies["AdminVisits"] ?? "0") + 1;
        Response.Cookies.Append("AdminVisits", visits.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });
        ViewBag.Visits = visits;

        const int pageSize = 5;
        var query = _db.Products.Include(p => p.Brand).Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(p => p.ProductName.Contains(keyword));

        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var total = await query.CountAsync();
        var list = await query.OrderByDescending(p => p.ProductId)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Keyword = keyword;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "CategoryId", "CategoryName");

        // Nếu gọi qua AJAX thì chỉ render lại bảng dữ liệu
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_ProductTablePartial", list);
        }

        return View(list);
    }

    // 2. Thêm mới sản phẩm (GET)
    public async Task<IActionResult> Create()
    {
        ViewBag.Brands = new SelectList(await _db.Brands.ToListAsync(), "BrandId", "BrandName");
        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "CategoryId", "CategoryName");
        return View(new ProductEditViewModel());
    }

    // 2. Thêm mới sản phẩm (POST + Upload ảnh + Validation)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Brands = new SelectList(await _db.Brands.ToListAsync(), "BrandId", "BrandName");
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "CategoryId", "CategoryName");
            return View(vm);
        }

        string? imgPath = null;
        if (vm.UploadedImage != null && vm.UploadedImage.Length > 0)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(vm.UploadedImage.FileName)}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await vm.UploadedImage.CopyToAsync(stream);
            }
            imgPath = $"/uploads/{fileName}";
        }

        var p = new Product
        {
            ProductName = vm.ProductName,
            BrandId = vm.BrandId,
            CategoryId = vm.CategoryId,
            Price = vm.Price,
            DiscountPrice = vm.DiscountPrice,
            StockQuantity = vm.StockQuantity,
            Description = vm.Description,
            ThumbnailUrl = imgPath ?? "/images/default.png",
            IsActive = true
        };

        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        // Lưu thông báo vào Session
        HttpContext.Session.SetString("LastAction", $"Vừa thêm thành công sản phẩm: {p.ProductName}");

        return RedirectToAction(nameof(Index));
    }

    // 3. Chỉnh sửa sản phẩm
    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Products.FindAsync(id);
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
            ExistingThumbnail = p.ThumbnailUrl
        };

        ViewBag.Brands = new SelectList(await _db.Brands.ToListAsync(), "BrandId", "BrandName", p.BrandId);
        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "CategoryId", "CategoryName", p.CategoryId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Brands = new SelectList(await _db.Brands.ToListAsync(), "BrandId", "BrandName");
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "CategoryId", "CategoryName");
            return View(vm);
        }

        var p = await _db.Products.FindAsync(vm.ProductId);
        if (p == null) return NotFound();

        if (vm.UploadedImage != null && vm.UploadedImage.Length > 0)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(vm.UploadedImage.FileName)}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await vm.UploadedImage.CopyToAsync(stream);
            }
            p.ThumbnailUrl = $"/uploads/{fileName}";
        }

        p.ProductName = vm.ProductName;
        p.BrandId = vm.BrandId;
        p.CategoryId = vm.CategoryId;
        p.Price = vm.Price;
        p.DiscountPrice = vm.DiscountPrice;
        p.StockQuantity = vm.StockQuantity;
        p.Description = vm.Description;

        await _db.SaveChangesAsync();
        HttpContext.Session.SetString("LastAction", $"Cập nhật thành công: {p.ProductName}");
        return RedirectToAction(nameof(Index));
    }

    // 4. Xóa sản phẩm
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null)
        {
            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false, message = "Không tìm thấy" });
    }
}