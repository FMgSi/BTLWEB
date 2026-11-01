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
    private readonly IInstallmentService _installmentService;
    private readonly IWebHostEnvironment _env;

    private string SettingsFilePath => Path.Combine(_env.ContentRootPath, "App_Data", "systemsettings.json");
    private string DiscountsFilePath => Path.Combine(_env.ContentRootPath, "App_Data", "discounts.json");

    private SystemSettingsViewModel LoadSettings()
    {
        try
        {
            if (System.IO.File.Exists(SettingsFilePath))
            {
                var json = System.IO.File.ReadAllText(SettingsFilePath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<SystemSettingsViewModel>(json);
                if (settings != null) return settings;
            }
        }
        catch { }
        return new SystemSettingsViewModel();
    }

    private bool SaveSettingsToFile(SystemSettingsViewModel settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(SettingsFilePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private List<DiscountDto> LoadDiscounts()
    {
        try
        {
            if (System.IO.File.Exists(DiscountsFilePath))
            {
                var json = System.IO.File.ReadAllText(DiscountsFilePath);
                var list = System.Text.Json.JsonSerializer.Deserialize<List<DiscountDto>>(json);
                // Giữ nguyên danh sách đã lưu (kể cả rỗng khi người dùng đã xóa hết, không tự nạp đè)
                if (list != null) return list;
            }
            else
            {
                // Chỉ nạp mẫu lần đầu tiên khi chưa có file cấu hình
                var defaults = GetDefaultDiscounts();
                SaveDiscountsToFile(defaults);
                return defaults;
            }
        }
        catch { }
        return new List<DiscountDto>();
    }

    private bool SaveDiscountsToFile(List<DiscountDto> list)
    {
        try
        {
            var dir = Path.GetDirectoryName(DiscountsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(DiscountsFilePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<DiscountDto> GetDefaultDiscounts() => new()
    {
        new DiscountDto { Code = "LAPTOPMOI", Title = "Giảm giá laptop mới", DiscountType = "Giảm theo %", DiscountValueText = "5% (Tối đa 500k)", ConditionText = "Đơn từ 15tr", UsageText = "45 / 100", ExpiryText = "15/10/2026 - 30/11/2026", Status = "Đang hoạt động", StatusBadgeClass = "badge-in-stock" },
        new DiscountDto { Code = "TRIAN99", Title = "Tri ân khách hàng", DiscountType = "Giảm số tiền", DiscountValueText = "200.000đ", ConditionText = "Đơn từ 10tr", UsageText = "89 / 200", ExpiryText = "01/10/2026 - 31/10/2026", Status = "Đang hoạt động", StatusBadgeClass = "badge-in-stock" },
        new DiscountDto { Code = "GAMINGX", Title = "Đại tiệc Laptop Gaming", DiscountType = "Giảm số tiền", DiscountValueText = "1.000.000đ", ConditionText = "Mua dòng Gaming", UsageText = "10 / 10", ExpiryText = "10/10/2026 - 15/10/2026", Status = "Hết hạn", StatusBadgeClass = "badge-out-of-stock" },
        new DiscountDto { Code = "WELCOME", Title = "Chào mừng thành viên mới", DiscountType = "Giảm theo %", DiscountValueText = "3% (Tối đa 200k)", ConditionText = "Đơn hàng đầu tiên", UsageText = "120 / Không giới hạn", ExpiryText = "Không giới hạn", Status = "Đang hoạt động", StatusBadgeClass = "badge-in-stock" }
    };

    public AdminController(
        AppDbContext db,
        IOrderService orderService,
        IProductService productService,
        IInstallmentService installmentService,
        IWebHostEnvironment env)
    {
        _db = db;
        _orderService = orderService;
        _productService = productService;
        _installmentService = installmentService;
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

        if (user == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash))
        {
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
            return View();
        }

        if (!user.IsActive)
        {
            ViewBag.Error = "Tài khoản quản trị của bạn đã bị tạm khóa. Vui lòng liên hệ quản lý cấp cao.";
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
    /// Quản lý Hợp đồng Trả góp (Mốc 1: Theo dõi đơn chờ trả trước, đang trả, quá hạn, đã tất toán)
    /// GET: /Admin/Installments
    /// </summary>
    public async Task<IActionResult> Installments(string? search, string? status, int page = 1)
    {
        int pageSize = 15;
        var pagedResult = await _installmentService.GetContractsAsync(search, status, page, pageSize);
        var (waitingPrepay, activeContracts, totalDebt) = await _installmentService.GetInstallmentDashboardMetricsAsync();

        ViewBag.Search = search;
        ViewBag.Status = status ?? "all";
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(pagedResult.TotalItems / (double)pageSize);
        if (ViewBag.TotalPages == 0) ViewBag.TotalPages = 1;
        ViewBag.TotalCount = pagedResult.TotalItems;

        ViewBag.WaitingPrepayCount = waitingPrepay;
        ViewBag.ActiveContractsCount = activeContracts;
        ViewBag.TotalInstallmentDebt = totalDebt;

        return View(pagedResult.Items);
    }

    /// <summary>
    /// Xem chi tiết hợp đồng trả góp kèm lịch các kỳ và lịch sử biên nhận thanh toán
    /// GET: /Admin/InstallmentDetails/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> InstallmentDetails(int id)
    {
        var contract = await _installmentService.GetContractByIdAsync(id);
        if (contract == null)
        {
            TempData["Error"] = "Không tìm thấy hợp đồng trả góp yêu cầu.";
            return RedirectToAction(nameof(Installments));
        }

        return View(contract);
    }

    /// <summary>
    /// Admin xác nhận đã nhận khoản tiền trả trước 30% -> Kích hoạt hợp đồng và chốt lịch trả chính thức
    /// POST: /Admin/ActivateInstallment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateInstallment(int contractId, string paymentMethod, string? transactionRef, string? note, string? returnUrl = null)
    {
        var adminName = User.Identity?.Name ?? "Quản trị viên";
        var (success, message, contract) = await _installmentService.ActivateContractAsync(
            contractId, adminName, paymentMethod ?? "Tiền mặt", transactionRef, note);

        if (success)
        {
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Installments));
    }

    /// <summary>
    /// Admin thu tiền cho từng kỳ trả góp theo thứ tự
    /// POST: /Admin/CollectInstallmentPeriod
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CollectInstallmentPeriod(
        int contractId, int periodNumber, string paymentMethod, string? transactionRef, string? note, string? returnUrl = null)
    {
        var adminName = User.Identity?.Name ?? "Quản trị viên";
        var (success, message, schedule) = await _installmentService.CollectPeriodPaymentAsync(
            contractId, periodNumber, adminName, paymentMethod ?? "Tiền mặt", transactionRef, note);

        if (success)
        {
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(InstallmentDetails), new { id = contractId });
    }

    /// <summary>
    /// In hoặc xem Biên nhận thu tiền trả góp
    /// GET: /Admin/InstallmentReceipt/{paymentId}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> InstallmentReceipt(int id)
    {
        var payment = await _db.Payments
            .AsNoTracking()
            .Include(p => p.Order)
                .ThenInclude(o => o!.InstallmentContract)
            .Include(p => p.Schedule)
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null)
        {
            return NotFound("Không tìm thấy biên nhận thu tiền.");
        }

        return View(payment);
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

            var phone = !string.IsNullOrWhiteSpace(u.PhoneNumber)
                ? u.PhoneNumber
                : (userOrders.OrderByDescending(o => o.CreatedAt).FirstOrDefault()?.PhoneNumber ?? "Chưa cập nhật");

            dtoList.Add(new CustomerDto
            {
                CustomerCode = $"KH{u.UserId:D3}",
                FullName = string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName,
                PhoneNumber = phone,
                Email = u.Email,
                TotalOrders = userOrders.Count,
                TotalSpent = totalSpent,
                MembershipTier = membershipTier,
                TierBadgeClass = tierClass,
                IsActive = u.IsActive
            });
        }

        var query = dtoList.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower().Trim();
            query = query.Where(c => c.FullName.ToLower().Contains(s) || c.Email.ToLower().Contains(s) || c.CustomerCode.ToLower().Contains(s) || c.PhoneNumber.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            if (status == "active") query = query.Where(c => c.IsActive);
            else if (status == "locked") query = query.Where(c => !c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(tier) && tier != "all")
        {
            query = query.Where(c => c.MembershipTier.Equals(tier, StringComparison.OrdinalIgnoreCase));
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
    /// Khóa / Mở khóa tài khoản khách hàng thật trong cơ sở dữ liệu
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCustomerLock(string customerCode)
    {
        int userId = 0;
        if (customerCode.StartsWith("KH", StringComparison.OrdinalIgnoreCase) && int.TryParse(customerCode[2..], out int parsed))
        {
            userId = parsed;
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId || u.Username == customerCode || u.Email == customerCode);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            return Json(new { success = true, isActive = user.IsActive, message = user.IsActive ? "Đã mở khóa tài khoản khách hàng thành công" : "Đã khóa tài khoản khách hàng thành công" });
        }
        return Json(new { success = false, message = "Không tìm thấy khách hàng trong cơ sở dữ liệu" });
    }

    /// <summary>
    /// Quản lý Mã giảm giá
    /// GET: /Admin/Discounts
    /// </summary>
    public IActionResult Discounts(string? search, string? status, string? discountType, int page = 1)
    {
        var discounts = LoadDiscounts();
        var query = discounts.AsQueryable();

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

        var discounts = LoadDiscounts();
        discounts.Insert(0, new DiscountDto
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
        if (SaveDiscountsToFile(discounts))
        {
            TempData["Success"] = $"Đã tạo mã giảm giá {code.ToUpper()} thành công!";
        }
        else
        {
            TempData["Error"] = "Không thể ghi tệp mã giảm giá. Vui lòng kiểm tra quyền thư mục App_Data!";
        }
        return RedirectToAction(nameof(Discounts));
    }

    /// <summary>
    /// POST: /Admin/DeleteDiscount
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteDiscount(string code)
    {
        var discounts = LoadDiscounts();
        var item = discounts.FirstOrDefault(d => d.Code == code);
        if (item != null)
        {
            discounts.Remove(item);
            if (SaveDiscountsToFile(discounts))
            {
                return Json(new { success = true, message = "Đã xóa mã giảm giá thành công" });
            }
            return Json(new { success = false, message = "Lỗi khi ghi tệp lưu trữ mã giảm giá" });
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
        return View(LoadSettings());
    }

    /// <summary>
    /// POST: /Admin/SaveSettings
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveSettings(SystemSettingsViewModel model, string activeTab = "payments")
    {
        if (SaveSettingsToFile(model))
        {
            TempData["Success"] = "Đã lưu toàn bộ cấu hình hệ thống thành công!";
        }
        else
        {
            TempData["Error"] = "Lỗi khi ghi tệp cấu hình hệ thống. Vui lòng kiểm tra quyền thư mục App_Data!";
        }
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

    /// <summary>
    /// API trả dữ liệu thông báo thời gian thực cho Dropdown trên Topbar
    /// GET: /Admin/GetNotifications
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        try
        {
            var now = DateTime.UtcNow;
            var sevenDaysLater = now.AddDays(7);

            // 1. Đơn hàng mới / Chờ xử lý / Chờ trả trước
            var pendingOrders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.OrderStatus == "Chờ xử lý" || o.PaymentStatus == "Chờ trả trước")
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new
                {
                    type = "order",
                    id = o.OrderId,
                    code = o.OrderCode,
                    title = o.PurchaseMode == "Installment"
                        ? $"Đơn trả góp #{o.OrderCode}"
                        : $"Đơn hàng mới #{o.OrderCode}",
                    subtitle = $"{o.CustomerName} • {o.TotalAmount:N0}₫",
                    badge = o.PurchaseMode == "Installment" ? "Chờ trả trước" : "Chờ xử lý",
                    badgeClass = o.PurchaseMode == "Installment" ? "bg-warning text-dark" : "bg-primary text-white",
                    url = o.PurchaseMode == "Installment"
                        ? $"/Admin/Installments?search={o.OrderCode}"
                        : $"/Admin/Orders?search={o.OrderCode}",
                    time = o.CreatedAt.ToLocalTime().ToString("HH:mm dd/MM")
                })
                .ToListAsync();

            // 2. Kỳ trả góp đến hạn hoặc quá hạn
            var urgentSchedules = await _db.InstallmentSchedules
                .AsNoTracking()
                .Include(s => s.Contract)
                    .ThenInclude(c => c!.Order)
                .Where(s => s.Status != "Đã thanh toán" && s.DueDate <= sevenDaysLater)
                .OrderBy(s => s.DueDate)
                .Take(5)
                .Select(s => new
                {
                    type = "installment",
                    id = s.ScheduleId,
                    contractId = s.ContractId,
                    code = s.Contract != null ? s.Contract.ContractCode : "",
                    title = s.DueDate < now
                        ? $"Quá hạn kỳ {s.PeriodNumber} - {(s.Contract != null ? s.Contract.ContractCode : "")}"
                        : $"Đến hạn kỳ {s.PeriodNumber} - {(s.Contract != null ? s.Contract.ContractCode : "")}",
                    subtitle = $"{(s.Contract != null && s.Contract.Order != null ? s.Contract.Order.CustomerName : "Khách hàng")} • {s.Amount:N0}₫",
                    badge = s.DueDate < now ? "Quá hạn" : "Sắp đến hạn",
                    badgeClass = s.DueDate < now ? "bg-danger text-white" : "bg-warning text-dark",
                    url = $"/Admin/InstallmentDetails/{s.ContractId}",
                    time = s.DueDate.ToLocalTime().ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            // 3. Cảnh báo tồn kho thấp (<= 2 máy)
            var lowStockItems = await _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.StockQuantity <= 2)
                .OrderBy(p => p.StockQuantity)
                .Take(3)
                .Select(p => new
                {
                    type = "stock",
                    id = p.ProductId,
                    code = $"SP{p.ProductId:D3}",
                    title = p.StockQuantity == 0 ? $"Hết hàng: {p.ProductName}" : $"Sắp hết hàng: {p.ProductName}",
                    subtitle = $"Tồn kho hiện tại: {p.StockQuantity} máy",
                    badge = p.StockQuantity == 0 ? "Hết hàng" : "Tồn thấp",
                    badgeClass = p.StockQuantity == 0 ? "bg-danger text-white" : "bg-secondary text-white",
                    url = $"/Product/Edit/{p.ProductId}",
                    time = "Kho hàng"
                })
                .ToListAsync();

            int pendingOrdersCount = await _db.Orders
                .CountAsync(o => o.OrderStatus == "Chờ xử lý" || o.PaymentStatus == "Chờ trả trước");
            int urgentSchedulesCount = await _db.InstallmentSchedules
                .CountAsync(s => s.Status != "Đã thanh toán" && s.DueDate <= sevenDaysLater);
            int lowStockCount = await _db.Products
                .CountAsync(p => p.IsActive && p.StockQuantity <= 2);

            int totalCount = pendingOrdersCount + urgentSchedulesCount + lowStockCount;

            return Json(new
            {
                success = true,
                totalCount,
                orders = pendingOrders,
                installments = urgentSchedules,
                stocks = lowStockItems
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message, totalCount = 0 });
        }
    }
}
