using LaptopStore.Data;
using LaptopStore.Helpers;
using LaptopStore.Models;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace LaptopStore.Controllers;

/// <summary>
/// Controller Quản trị Trung tâm (Admin Portal)
/// Được bảo vệ chặt chẽ bởi cơ chế Xác thực & Phân quyền Cookie Authentication [Authorize(Roles = "Admin")]
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IWebHostEnvironment _env;

    // Bộ nhớ cấu hình tạm thời mô phỏng cơ sở dữ liệu cấu hình
    private static SystemSettingsViewModel _systemSettings = new();

    // Dữ liệu khách hàng mẫu hiển thị bổ sung nếu CSDL chưa có nhiều
    private static List<CustomerDto> _mockCustomers = new()
    {
        new CustomerDto { CustomerCode = "KH001", FullName = "Nguyễn An Bình", PhoneNumber = "0901234567", Email = "anbinh@gmail.com", TotalOrders = 12, TotalSpent = 145000000m, MembershipTier = "Kim cương", TierBadgeClass = "badge-tier-diamond", IsActive = true },
        new CustomerDto { CustomerCode = "KH002", FullName = "Trần Thị Hoa", PhoneNumber = "0987654321", Email = "thithoa@yahoo.com", TotalOrders = 5, TotalSpent = 48200000m, MembershipTier = "Vàng", TierBadgeClass = "badge-tier-gold", IsActive = true },
        new CustomerDto { CustomerCode = "KH003", FullName = "Lê Hoàng Nam", PhoneNumber = "0912345678", Email = "namle@gmail.com", TotalOrders = 1, TotalSpent = 12500000m, MembershipTier = "Đồng", TierBadgeClass = "badge-tier-bronze", IsActive = true },
        new CustomerDto { CustomerCode = "KH004", FullName = "Phạm Văn Đức", PhoneNumber = "0933445566", Email = "ducpham@gmail.com", TotalOrders = 3, TotalSpent = 32000000m, MembershipTier = "Bạc", TierBadgeClass = "badge-tier-silver", IsActive = false },
        new CustomerDto { CustomerCode = "KH005", FullName = "Vũ Mỹ Linh", PhoneNumber = "0977889900", Email = "mylinh@gmail.com", TotalOrders = 8, TotalSpent = 78900000m, MembershipTier = "Vàng", TierBadgeClass = "badge-tier-gold", IsActive = true }
    };

    // Dữ liệu mã giảm giá mẫu
    private static List<DiscountDto> _mockDiscounts = new()
    {
        new DiscountDto { Code = "LAPTOPMOI", Title = "Giảm giá laptop mới", DiscountType = "Giảm theo %", DiscountValueText = "5% (Tối đa 500k)", ConditionText = "Đơn từ 15tr", UsageText = "45 / 100", ExpiryText = "15/10/2026 - 30/11/2026", Status = "Đang hoạt động", StatusBadgeClass = "badge-in-stock" },
        new DiscountDto { Code = "TRIAN99", Title = "Tri ân khách hàng", DiscountType = "Giảm số tiền", DiscountValueText = "200.000đ", ConditionText = "Đơn từ 10tr", UsageText = "89 / 200", ExpiryText = "01/10/2026 - 31/10/2026", Status = "Đang hoạt động", StatusBadgeClass = "badge-in-stock" },
        new DiscountDto { Code = "GAMINGX", Title = "Đại tiệc Laptop Gaming", DiscountType = "Giảm số tiền", DiscountValueText = "1.000.000đ", ConditionText = "Mua dòng Gaming", UsageText = "10 / 10", ExpiryText = "10/10/2026 - 15/10/2026", Status = "Hết hạn", StatusBadgeClass = "badge-out-of-stock" },
        new DiscountDto { Code = "WELCOME", Title = "Chào mừng thành viên mới", DiscountType = "Giảm theo %", DiscountValueText = "3% (Tối đa 200k)", ConditionText = "Đơn hàng đầu tiên", UsageText = "120 / Không giới hạn", ExpiryText = "Không giới hạn", Status = "Đang hoạt động", StatusBadgeClass = "badge-in-stock" }
    };

    public AdminController(AppDbContext db, IOrderService orderService, IProductService productService, IWebHostEnvironment env)
    {
        _db = db;
        _orderService = orderService;
        _productService = productService;
        _env = env;
    }

    /// <summary>
    /// Giao diện Đăng nhập Trang Quản trị (GET: /Admin/Login)
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Product");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// Xử lý Đăng nhập Quản trị viên (POST: /Admin/Login)
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
            return View();
        }

        var normalizedInput = username.Trim().ToLower();

        // 1. Tìm tài khoản trong database
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == normalizedInput || u.Email.ToLower() == normalizedInput);

        bool isValid = false;
        if (user != null)
        {
            isValid = PasswordHelper.VerifyPassword(password, user.PasswordHash);
            // Tiện ích cho môi trường đồ án: Chấp nhận cả mật khẩu "Admin@123" hoặc "admin" cho tài khoản quản trị
            if (!isValid && (password == "Admin@123" || password == "admin") && user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                isValid = true;
            }
        }
        else if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && (password == "Admin@123" || password == "admin"))
        {
            // Dự phòng nếu bảng Users chưa có bản ghi
            user = new User
            {
                UserId = 1,
                Username = "admin",
                Email = "admin@pcstore.vn",
                FullName = "Quản Trị Viên",
                Role = "Admin"
            };
            isValid = true;
        }

        if (!isValid || user == null)
        {
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
            return View();
        }

        if (!user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            ViewBag.Error = "Tài khoản của bạn không có quyền truy cập Trang Quản Trị.";
            return View();
        }

        // 2. Thiết lập Claims và cấp Cookie đăng nhập
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, "Admin")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        // Lưu thông tin vào Session để tương thích ngược
        HttpContext.Session.SetString("UserId", user.UserId.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Role", "Admin");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Product");
    }

    /// <summary>
    /// Đăng xuất khỏi Trang Quản trị (POST / GET: /Admin/Logout)
    /// </summary>
    [HttpPost]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Trang thông báo không có quyền truy cập (GET: /Admin/AccessDenied)
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// GET: /Admin hoặc /Admin/Index -> chuyển hướng sang Quản lý sản phẩm
    /// </summary>
    public IActionResult Index() => RedirectToAction("Index", "Product");

    /// <summary>
    /// Dashboard Thống kê thời gian thực từ Cơ sở dữ liệu
    /// GET: /Admin/Dashboard
    /// </summary>
    public async Task<IActionResult> Dashboard()
    {
        var model = await _orderService.GetDashboardStatsAsync();
        return View(model);
    }

    /// <summary>
    /// Quản lý Đơn hàng - Dữ liệu thực từ Cơ sở dữ liệu Microsoft SQL Server
    /// GET: /Admin/Orders
    /// </summary>
    public async Task<IActionResult> Orders(string? search, string? status, int page = 1)
    {
        int pageSize = 15;
        var pagedResult = await _orderService.GetOrdersAsync(search, status, page, pageSize);

        ViewBag.Search = search;
        ViewBag.Status = status ?? "all";
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(pagedResult.TotalItems / (double)pageSize);
        if (ViewBag.TotalPages == 0) ViewBag.TotalPages = 1;
        ViewBag.TotalCount = pagedResult.TotalItems;

        return View(pagedResult.Items);
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng (Đang xử lý -> Đã xác nhận -> Đã giao / Hoàn thành / Đã hủy)
    /// POST: /Admin/UpdateOrderStatus
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, string status, string? note)
    {
        var adminName = User.Identity?.Name ?? "Quản trị viên";
        var result = await _orderService.UpdateOrderStatusAsync(orderId, status, note, adminName);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Orders));
    }

    /// <summary>
    /// Quản lý Khách hàng
    /// GET: /Admin/Customers
    /// </summary>
    public async Task<IActionResult> Customers(string? search, string? status, string? tier, int page = 1)
    {
        // Lấy danh sách khách hàng thật từ CSDL
        var realCustomers = await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == "Customer")
            .ToListAsync();

        var dtoList = new List<CustomerDto>();

        foreach (var u in realCustomers)
        {
            var userOrders = await _db.Orders.Where(o => o.UserId == u.UserId).ToListAsync();
            decimal totalSpent = userOrders.Where(o => o.PaymentStatus == "Đã thanh toán").Sum(o => o.TotalAmount);
            string membershipTier = totalSpent >= 100000000 ? "Kim cương" : (totalSpent >= 50000000 ? "Vàng" : (totalSpent >= 20000000 ? "Bạc" : "Đồng"));
            string tierClass = membershipTier == "Kim cương" ? "badge-tier-diamond" : (membershipTier == "Vàng" ? "badge-tier-gold" : "badge-tier-silver");

            dtoList.Add(new CustomerDto
            {
                CustomerCode = $"KH{u.UserId:D3}",
                FullName = string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName,
                PhoneNumber = "098" + (1000000 + u.UserId),
                Email = u.Email,
                TotalOrders = userOrders.Count,
                TotalSpent = totalSpent,
                MembershipTier = membershipTier,
                TierBadgeClass = tierClass,
                IsActive = true
            });
        }

        // Ghép thêm mock mẫu nếu mới setup CSDL
        if (dtoList.Count < _mockCustomers.Count)
        {
            foreach (var m in _mockCustomers)
            {
                if (!dtoList.Any(d => d.CustomerCode == m.CustomerCode))
                {
                    dtoList.Add(m);
                }
            }
        }

        var query = dtoList.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower().Trim();
            query = query.Where(c => c.FullName.ToLower().Contains(s) || c.Email.ToLower().Contains(s) || c.CustomerCode.ToLower().Contains(s));
        }

        var list = query.ToList();
        ViewBag.Search = search;
        ViewBag.Status = status ?? "all";
        ViewBag.Tier = tier ?? "all";
        ViewBag.TotalCount = list.Count;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = 1;

        return View(list);
    }

    /// <summary>
    /// POST: /Admin/ToggleCustomerLock
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleCustomerLock(string customerCode)
    {
        var cust = _mockCustomers.FirstOrDefault(c => c.CustomerCode == customerCode);
        if (cust != null)
        {
            cust.IsActive = !cust.IsActive;
            return Json(new { success = true, isActive = cust.IsActive, message = cust.IsActive ? "Đã mở khóa khách hàng thành công" : "Đã khóa khách hàng thành công" });
        }
        return Json(new { success = false, message = "Không tìm thấy khách hàng" });
    }

    /// <summary>
    /// Quản lý Mã giảm giá
    /// GET: /Admin/Discounts
    /// </summary>
    public IActionResult Discounts(string? search, string? status, string? discountType, int page = 1)
    {
        var query = _mockDiscounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower().Trim();
            query = query.Where(d => d.Code.ToLower().Contains(s) || d.Title.ToLower().Contains(s));
        }

        var list = query.ToList();
        ViewBag.Search = search;
        ViewBag.Status = status ?? "all";
        ViewBag.DiscountType = discountType ?? "all";
        ViewBag.TotalCount = list.Count;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = 1;

        return View(list);
    }

    /// <summary>
    /// POST: /Admin/CreateDiscount
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateDiscount(string code, string title, string discountType, string value, string condition, int quantity)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["Error"] = "Mã voucher không được để trống";
            return RedirectToAction(nameof(Discounts));
        }

        _mockDiscounts.Insert(0, new DiscountDto
        {
            Code = code.ToUpper().Trim(),
            Title = title,
            DiscountType = discountType == "percent" ? "Giảm theo %" : "Giảm số tiền",
            DiscountValueText = discountType == "percent" ? $"{value}%" : $"{value}đ",
            ConditionText = condition,
            UsageText = $"0 / {quantity}",
            ExpiryText = DateTime.Now.ToString("dd/MM/yyyy") + " - " + DateTime.Now.AddDays(30).ToString("dd/MM/yyyy"),
            Status = "Đang hoạt động",
            StatusBadgeClass = "badge-in-stock"
        });

        TempData["Success"] = $"Đã tạo mã giảm giá {code.ToUpper()} thành công!";
        return RedirectToAction(nameof(Discounts));
    }

    /// <summary>
    /// POST: /Admin/DeleteDiscount
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteDiscount(string code)
    {
        var item = _mockDiscounts.FirstOrDefault(d => d.Code == code);
        if (item != null)
        {
            _mockDiscounts.Remove(item);
            return Json(new { success = true, message = "Đã xóa mã giảm giá thành công" });
        }
        return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
    }

    /// <summary>
    /// Cấu hình Hệ thống
    /// GET: /Admin/Settings
    /// </summary>
    public IActionResult Settings(string activeTab = "payments")
    {
        ViewBag.ActiveTab = activeTab;
        return View(_systemSettings);
    }

    /// <summary>
    /// POST: /Admin/SaveSettings
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveSettings(SystemSettingsViewModel model, string activeTab = "payments")
    {
        _systemSettings = model;
        TempData["Success"] = "Đã lưu toàn bộ cấu hình hệ thống thành công!";
        return RedirectToAction(nameof(Settings), new { activeTab });
    }

    /// <summary>
    /// Báo cáo thống kê
    /// GET: /Admin/Reports
    /// </summary>
    public async Task<IActionResult> Reports()
    {
        var model = await _orderService.GetDashboardStatsAsync();
        return View(model);
    }

    /// <summary>
    /// Tiện ích Xuất dữ liệu sản phẩm sang file CSV
    /// </summary>
    public async Task<IActionResult> ExportProducts()
    {
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Mã SP,Tên sản phẩm,Thương hiệu,Danh mục,Giá niêm yết,Giá khuyến mãi,Kho số lượng,Trạng thái");

        foreach (var p in products)
        {
            var status = p.StockQuantity > 5 ? "Còn hàng" : (p.StockQuantity > 0 ? "Sắp hết hàng" : "Hết hàng");
            csv.AppendLine($"\"SP{p.ProductId:D3}\",\"{p.ProductName.Replace("\"", "\"\"")}\",\"{p.Brand?.BrandName}\",\"{p.Category?.CategoryName}\",{p.Price},{p.DiscountPrice ?? 0},{p.StockQuantity},\"{status}\"");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"DanhSach_SanPham_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }
}
