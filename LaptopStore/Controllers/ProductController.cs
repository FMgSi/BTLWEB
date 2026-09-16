using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers;

/// <summary>
/// Controller Quản trị (Admin) dành cho việc quản lý sản phẩm Laptop
/// Áp dụng mô hình ASP.NET Core MVC truyền thống (Model - View - Controller)
/// Đường dẫn truy cập: /Product
/// </summary>
public class ProductController : Controller
{
    // Đối tượng DbContext để tương tác với cơ sở dữ liệu MySQL
    private readonly AppDbContext _db;
    
    // Đối tượng môi trường để lấy đường dẫn thư mục gốc wwwroot (dùng khi upload ảnh)
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Hàm khởi tạo (Constructor) sử dụng kỹ thuật Dependency Injection để nhận DbContext và WebHostEnvironment
    /// </summary>
    public ProductController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// 1. Hiển thị danh sách sản phẩm cho Admin (hỗ trợ Tìm kiếm từ khóa, Lọc danh mục và Phân trang AJAX)
    /// GET: /Product?keyword=...&categoryId=...&page=1
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm theo tên sản phẩm</param>
    /// <param name="categoryId">ID danh mục cần lọc (null nếu xem tất cả)</param>
    /// <param name="page">Số thứ tự trang hiện tại (mặc định là 1)</param>
    public async Task<IActionResult> Index(string? keyword, int? categoryId, int page = 1)
    {
        // Ghi nhận số lượt truy cập trang Admin thông qua Cookie (lưu trong 7 ngày)
        var visits = int.Parse(Request.Cookies["AdminVisits"] ?? "0") + 1;
        Response.Cookies.Append("AdminVisits", visits.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });
        ViewBag.Visits = visits;

        // Số lượng sản phẩm hiển thị trên mỗi trang bảng Admin
        const int pageSize = 5;

        // Khởi tạo câu truy vấn từ bảng Products kèm thông tin Hãng (Brand) và Danh mục (Category)
        var query = _db.Products.Include(p => p.Brand).Include(p => p.Category).AsQueryable();

        // Lọc theo từ khóa tìm kiếm nếu có
        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(p => p.ProductName.Contains(keyword));

        // Lọc theo danh mục nếu người dùng chọn
        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        // Đếm tổng số lượng bản ghi thỏa mãn điều kiện
        var total = await query.CountAsync();

        // Lấy danh sách sản phẩm của trang hiện tại (sắp xếp ID giảm dần - sản phẩm mới nhất lên đầu)
        var list = await query.OrderByDescending(p => p.ProductId)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        // Truyền các thông tin phân trang và bộ lọc sang View bằng ViewBag
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Keyword = keyword;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "CategoryId", "CategoryName");

        // Nếu yêu cầu được gửi từ Javascript AJAX, chỉ trả về phần bảng dữ liệu (_ProductTablePartial)
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_ProductTablePartial", list);
        }

        // Ngược lại, trả về toàn bộ giao diện trang Index
        return View(list);
    }

    /// <summary>
    /// 2. Giao diện Thêm mới sản phẩm (GET)
    /// GET: /Product/Create
    /// </summary>
    public async Task<IActionResult> Create()
    {
        // Chuẩn bị danh sách Hãng và Danh mục để hiển thị dưới dạng dropdown <select>
        ViewBag.Brands = new SelectList(await _db.Brands.ToListAsync(), "BrandId", "BrandName");
        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "CategoryId", "CategoryName");
        return View(new ProductEditViewModel());
    }

    /// <summary>
    /// 2. Xử lý lưu sản phẩm mới vào Database (POST)
    /// Hỗ trợ kiểm tra dữ liệu hợp lệ (Validation) và Upload file hình ảnh lên server
    /// POST: /Product/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken] // Chống tấn công CSRF (Cross-Site Request Forgery)
    public async Task<IActionResult> Create(ProductEditViewModel vm)
    {
        // Kiểm tra xem dữ liệu nhập vào form có hợp lệ theo các DataAnnotations không
        if (!ModelState.IsValid)
        {
            // Nếu không hợp lệ, nạp lại dropdown và hiển thị lại form kèm thông báo lỗi
            ViewBag.Brands = new SelectList(await _db.Brands.ToListAsync(), "BrandId", "BrandName");
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "CategoryId", "CategoryName");
            return View(vm);
        }

        // Xử lý upload file hình ảnh nếu người dùng có chọn file
        string? imgPath = null;
        if (vm.UploadedImage != null && vm.UploadedImage.Length > 0)
        {
            // Xác định thư mục lưu ảnh: wwwroot/uploads
            var folder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            // Tạo tên file duy nhất bằng Guid để tránh bị trùng đè tên file
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(vm.UploadedImage.FileName)}";
            var fullPath = Path.Combine(folder, fileName);

            // Lưu file ảnh vào ổ đĩa server
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await vm.UploadedImage.CopyToAsync(stream);
            }
            imgPath = $"/uploads/{fileName}";
        }

        // Tạo Entity Product mới từ dữ liệu ViewModel
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

        // Thêm vào DbContext và lưu xuống Database MySQL
        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        // Lưu thông báo thành công vào Session để hiển thị ở trang Index
        HttpContext.Session.SetString("LastAction", $"Vừa thêm thành công sản phẩm: {p.ProductName}");

        // Chuyển hướng quay về trang danh sách sản phẩm
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 3. Giao diện Chỉnh sửa sản phẩm (GET)
    /// GET: /Product/Edit/{id}
    /// </summary>
    /// <param name="id">Mã sản phẩm cần sửa</param>
    public async Task<IActionResult> Edit(int id)
    {
        // Tìm sản phẩm theo ID trong Database
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();

        // Ánh xạ dữ liệu từ Product Entity sang ViewModel để hiển thị lên Form
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
            ViewBag.Brands = new SelectList(await _db.Brands.ToListAsync(), "BrandId", "BrandName");
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "CategoryId", "CategoryName");
            return View(vm);
        }

        var p = await _db.Products.FindAsync(vm.ProductId);
        if (p == null) return NotFound();

        // Nếu người dùng chọn tải lên ảnh mới thì lưu ảnh mới và cập nhật đường dẫn
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

        // Cập nhật các trường thông tin của sản phẩm
        p.ProductName = vm.ProductName;
        p.BrandId = vm.BrandId;
        p.CategoryId = vm.CategoryId;
        p.Price = vm.Price;
        p.DiscountPrice = vm.DiscountPrice;
        p.StockQuantity = vm.StockQuantity;
        p.Description = vm.Description;

        // Lưu các thay đổi xuống cơ sở dữ liệu MySQL
        await _db.SaveChangesAsync();
        HttpContext.Session.SetString("LastAction", $"Cập nhật thành công: {p.ProductName}");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 4. Xóa sản phẩm khỏi Database thông qua AJAX (POST)
    /// POST: /Product/Delete/{id}
    /// </summary>
    /// <param name="id">Mã sản phẩm cần xóa</param>
    /// <returns>JSON chứa trạng thái kết quả { success: true/false }</returns>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null)
        {
            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa sản phẩm thành công" });
        }
        return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
    }
}