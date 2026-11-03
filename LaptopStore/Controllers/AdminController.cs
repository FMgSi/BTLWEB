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
/// Được bảo vệ chặt chẽ bởi cơ chế Xác thực & Phân quyền Cookie Authentication [Authorize(Roles = "Admin,Staff")]
/// </summary>
[Authorize(Roles = "Admin,Staff")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IInstallmentService _installmentService;
    private readonly IWarrantyService _warrantyService;
    private readonly IDiscountService _discountService;
    private readonly IWebHostEnvironment _env;

    private string SettingsFilePath => Path.Combine(_env.ContentRootPath, "App_Data", "systemsettings.json");

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

    public AdminController(
        AppDbContext db,
        IOrderService orderService,
        IProductService productService,
        IInstallmentService installmentService,
        IWarrantyService warrantyService,
        IDiscountService discountService,
        IWebHostEnvironment env)
    {
        _db = db;
        _orderService = orderService;
        _productService = productService;
        _installmentService = installmentService;
        _warrantyService = warrantyService;
        _discountService = discountService;
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
    /// GET: /Admin/Discounts
    /// Gỡ bỏ trang quản lý voucher, chuyển hướng an toàn về Báo cáo thống kê
    /// </summary>
    public IActionResult Discounts()
    {
        return RedirectToAction(nameof(Reports));
    }

    /// <summary>
    /// POST: /Admin/CreateDiscount
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDiscount(CreateDiscountInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Code))
        {
            TempData["Error"] = "Mã voucher không được để trống";
            return RedirectToAction(nameof(Discounts));
        }

        var (success, message, created) = await _discountService.CreateDiscountAsync(input, User.Identity?.Name ?? "Admin");
        if (success)
        {
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }

        return RedirectToAction(nameof(Discounts));
    }

    /// <summary>
    /// Bật / Tắt (Ngừng áp dụng) mã giảm giá
    /// POST: /Admin/ToggleDiscountStatus
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDiscountStatus(int id)
    {
        var (success, message) = await _discountService.ToggleDiscountStatusAsync(id, User.Identity?.Name ?? "Admin");
        return Json(new { success, message });
    }

    /// <summary>
    /// POST: /Admin/DeleteDiscount
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDiscount(int id)
    {
        var (success, message) = await _discountService.DeleteDiscountAsync(id, User.Identity?.Name ?? "Admin");
        return Json(new { success, message });
    }

    /// <summary>
    /// Cấu hình Hệ thống (Chỉ dành riêng cho Admin)
    /// GET: /Admin/Settings
    /// </summary>
    [Authorize(Roles = "Admin")]
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
    /// Báo cáo thống kê nâng cao đa chiều (Bán hàng, Dòng tiền thực thu trả góp, Sản phẩm biến thể, Dịch vụ bảo hành)
    /// GET: /Admin/Reports
    /// </summary>
    public async Task<IActionResult> Reports(string period = "month", DateTime? fromDate = null, DateTime? toDate = null)
    {
        DateTime start;
        DateTime end;

        switch (period?.ToLower())
        {
            case "today":
                start = DateTime.Today;
                end = DateTime.Today.AddDays(1).AddTicks(-1);
                break;
            case "7days":
                start = DateTime.Today.AddDays(-6);
                end = DateTime.Today.AddDays(1).AddTicks(-1);
                break;
            case "custom":
                start = fromDate ?? DateTime.Today.AddDays(-30);
                end = (toDate ?? DateTime.Today).AddDays(1).AddTicks(-1);
                break;
            case "month":
            default:
                period = "month";
                start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                end = DateTime.Today.AddDays(1).AddTicks(-1);
                break;
        }

        var report = new AdminReportsViewModel
        {
            Period = period,
            FromDate = start,
            ToDate = end
        };

        // 1. BÁO CÁO BÁN HÀNG TRONG KHOẢNG THỜI GIAN
        var ordersInRange = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.InstallmentContract)
            .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
            .ToListAsync();

        report.TotalOrders = ordersInRange.Count;
        report.NewOrders = ordersInRange.Count(o => o.OrderStatus == "Mới" || o.OrderStatus == "Chờ duyệt");
        report.ProcessingOrders = ordersInRange.Count(o => o.OrderStatus == "Đang xử lý" || o.OrderStatus == "Đang giao" || o.OrderStatus == "Đã xác nhận");
        report.CompletedOrders = ordersInRange.Count(o => o.OrderStatus == "Hoàn thành" || o.OrderStatus == "Đã giao");
        report.CancelledOrders = ordersInRange.Count(o => o.OrderStatus == "Đã hủy");

        var completedOrders = ordersInRange.Where(o => o.OrderStatus == "Hoàn thành" || o.OrderStatus == "Đã giao").ToList();
        report.TotalCompletedRevenue = completedOrders.Sum(o => o.TotalAmount);

        // Dòng tiền thực thu: Đơn thường hoàn tất + Tiền trả trước đơn trả góp + Các kỳ trả góp đã thu
        decimal normalCash = completedOrders.Where(o => o.PurchaseMode != "Installment").Sum(o => o.TotalAmount);
        decimal installmentPrepay = completedOrders.Where(o => o.PurchaseMode == "Installment")
            .Sum(o => o.InstallmentContract != null ? o.InstallmentContract.PrepayAmount : Math.Round(o.TotalAmount * 0.30m));

        decimal installmentPaidSchedules = 0;
        try
        {
            installmentPaidSchedules = await _db.InstallmentSchedules
                .AsNoTracking()
                .Where(s => s.Status == "Đã thanh toán" && s.PaidAt >= start && s.PaidAt <= end)
                .SumAsync(s => s.PaidAmount > 0 ? s.PaidAmount : s.Amount);
        }
        catch { }

        report.InstallmentCashCollected = installmentPrepay + installmentPaidSchedules;
        report.ActualCashCollected = normalCash + report.InstallmentCashCollected;

        // 2. BÁO CÁO SẢN PHẨM: SẢN PHẨM BÁN CHẠY (TOP SELLING)
        var completedOrderIds = completedOrders.Select(o => o.OrderId).ToList();
        var topSellingItems = await _db.OrderItems
            .AsNoTracking()
            .Where(i => completedOrderIds.Contains(i.OrderId))
            .GroupBy(i => new { i.ProductId, i.ProductName, i.SelectedSpecs })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                VariantSpecs = g.Key.SelectedSpecs ?? "",
                QuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(8)
            .ToListAsync();

        var topProductIds = topSellingItems.Select(t => t.ProductId).ToList();
        var productThumbnails = await _db.Products
            .AsNoTracking()
            .Where(p => topProductIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId, p => p.ThumbnailUrl ?? "");

        report.TopSellingProducts = topSellingItems.Select(t => new TopSellingProductDto
        {
            ProductId = t.ProductId,
            ProductName = t.ProductName,
            VariantSpecs = t.VariantSpecs,
            ThumbnailUrl = productThumbnails.ContainsKey(t.ProductId) ? productThumbnails[t.ProductId] : "",
            QuantitySold = t.QuantitySold,
            TotalRevenue = t.TotalRevenue
        }).ToList();

        // SẢN PHẨM SẮP HẾT HÀNG THEO CẤU HÌNH (STOCK <= 3)
        var lowStockBase = await _db.Products
            .AsNoTracking()
            .Include(p => p.Specification)
            .Where(p => p.IsActive && p.StockQuantity <= 3)
            .OrderBy(p => p.StockQuantity)
            .Take(6)
            .Select(p => new LowStockProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Configuration = p.Specification != null ? $"{p.Specification.CPU} • {p.Specification.RamGB}GB • {p.Specification.StorageGB}GB SSD" : "Tiêu chuẩn",
                StockQuantity = p.StockQuantity,
                Price = p.DiscountPrice ?? p.Price
            })
            .ToListAsync();

        var lowStockVariants = await _db.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Include(v => v.Color)
            .Where(v => v.IsActive && v.StockQuantity <= 3)
            .OrderBy(v => v.StockQuantity)
            .Take(6)
            .Select(v => new LowStockProductDto
            {
                ProductId = v.ProductId,
                ProductName = v.Product != null ? v.Product.ProductName : "Laptop",
                Configuration = $"[Biến thể {v.SKU}] Màu: {(v.Color != null ? v.Color.Name : "")} • RAM: {v.RamGB}GB • SSD: {v.StorageGB}GB",
                StockQuantity = v.StockQuantity,
                Price = v.DiscountPrice ?? v.Price ?? (v.Product != null ? v.Product.Price : 0m)
            })
            .ToListAsync();

        report.LowStockProducts = lowStockBase.Concat(lowStockVariants).OrderBy(x => x.StockQuantity).Take(10).ToList();

        // 3. BÁO CÁO DỊCH VỤ SỬA CHỮA & BẢO HÀNH
        var ticketsInRange = await _db.ServiceTickets
            .AsNoTracking()
            .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
            .ToListAsync();

        report.TotalReceivedTickets = ticketsInRange.Count;
        report.HandedOverTickets = ticketsInRange.Count(t => t.Status == "Đã bàn giao" || t.Status == "Đã trả máy" || t.Status == "Chờ trả máy");
        report.AwaitingQuoteTickets = await _db.ServiceTickets.AsNoTracking().CountAsync(t => t.Status == "Chờ duyệt báo giá");
        report.ProcessingTickets = await _db.ServiceTickets.AsNoTracking().CountAsync(t => t.Status == "Đã tiếp nhận" || t.Status == "Đang kiểm tra" || t.Status == "Đang xử lý");

        var completedTicketsWithTime = ticketsInRange
            .Where(t => (t.CompletedAt != null || t.ReturnedAt != null))
            .ToList();

        if (completedTicketsWithTime.Any())
        {
            report.AverageProcessingHours = Math.Round(completedTicketsWithTime
                .Average(t => ((t.ReturnedAt ?? t.CompletedAt!.Value) - t.CreatedAt).TotalHours), 1);
        }

        // 4. BIỂU ĐỒ DOANH THU THEO KHOẢNG THỜI GIAN
        if (period == "today")
        {
            for (int h = 8; h <= 20; h += 2)
            {
                var hStart = start.Date.AddHours(h);
                var hEnd = start.Date.AddHours(h + 2);
                var rev = completedOrders.Where(o => o.CreatedAt >= hStart && o.CreatedAt < hEnd).Sum(o => o.TotalAmount);
                report.ChartLabels.Add($"{h}h-{h + 2}h");
                report.ChartValues.Add(rev / 1000000m);
            }
        }
        else if (period == "7days")
        {
            for (int i = 6; i >= 0; i--)
            {
                var d = DateTime.Today.AddDays(-i);
                var rev = completedOrders.Where(o => o.CreatedAt.Date == d).Sum(o => o.TotalAmount);
                report.ChartLabels.Add(d.ToString("dd/MM"));
                report.ChartValues.Add(rev / 1000000m);
            }
        }
        else
        {
            var daysInMonth = DateTime.DaysInMonth(start.Year, start.Month);
            for (int day = 1; day <= Math.Min(DateTime.Today.Day, daysInMonth); day += 5)
            {
                var dStart = new DateTime(start.Year, start.Month, day);
                var dEnd = dStart.AddDays(5).AddTicks(-1);
                var rev = completedOrders.Where(o => o.CreatedAt >= dStart && o.CreatedAt <= dEnd).Sum(o => o.TotalAmount);
                report.ChartLabels.Add($"Ngày {day}-{Math.Min(day + 4, daysInMonth)}");
                report.ChartValues.Add(rev / 1000000m);
            }
        }

        return View(report);
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
            int pendingTicketsCount = await _db.ServiceTickets
                .CountAsync(t => t.Status == "Chờ tiếp nhận");

            int totalCount = pendingOrdersCount + urgentSchedulesCount + lowStockCount + pendingTicketsCount;

            return Json(new
            {
                success = true,
                totalCount,
                orders = pendingOrders,
                installments = urgentSchedules,
                stocks = lowStockItems,
                pendingTicketsCount
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message, totalCount = 0 });
        }
    }

    /// <summary>
    /// GET: /Admin/ServiceTickets
    /// Quản lý Hàng chờ tiếp nhận và Phiếu dịch vụ Bảo hành & Sửa chữa
    /// </summary>
    public async Task<IActionResult> ServiceTickets(string? status = "all", string? search = null, string? type = "all")
    {
        var tickets = await _warrantyService.GetAdminTicketsAsync(status, search, type);

        ViewBag.Status = status ?? "all";
        ViewBag.Search = search ?? "";
        ViewBag.Type = type ?? "all";

        var allTickets = await _warrantyService.GetAdminTicketsAsync("all", null, "all");
        ViewBag.PendingCount = allTickets.Count(t => t.Status == "Chờ tiếp nhận");
        ViewBag.ProcessingCount = allTickets.Count(t => t.Status == "Đang kiểm tra" || t.Status == "Đã tiếp nhận" || t.Status == "Chờ duyệt báo giá" || t.Status == "Đang xử lý");
        ViewBag.WaitingReturnCount = allTickets.Count(t => t.Status == "Chờ trả máy");
        ViewBag.TotalRevenue = allTickets.Where(t => t.Status == "Đã trả máy").Sum(t => t.FinalCost);

        return View(tickets);
    }

    /// <summary>
    /// GET: /Admin/ServiceTicketDetails/{id}
    /// Xem chi tiết phiếu dịch vụ, báo giá và lịch sử thao tác
    /// </summary>
    public async Task<IActionResult> ServiceTicketDetails(int id)
    {
        var ticket = await _warrantyService.GetTicketByIdAsync(id);
        if (ticket == null)
        {
            TempData["Error"] = "Không tìm thấy phiếu dịch vụ yêu cầu.";
            return RedirectToAction(nameof(ServiceTickets));
        }

        return View(ticket);
    }

    /// <summary>
    /// POST: /Admin/UpdateServiceTicket
    /// Cập nhật trạng thái phiếu tiếp nhận, tình trạng máy, phân công kỹ thuật viên
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateServiceTicket(
        int ticketId,
        string newStatus,
        string? notes,
        string? physicalCondition,
        string? accessories,
        string? rejectReason,
        decimal? finalCost,
        int? warrantyMonthsAfterRepair,
        string? returnUrl = null)
    {
        var adminName = User.Identity?.Name ?? "Nhân viên kỹ thuật";
        var (success, message) = await _warrantyService.UpdateTicketStatusByAdminAsync(
            ticketId, newStatus, adminName, notes, physicalCondition, accessories, rejectReason, finalCost, warrantyMonthsAfterRepair);

        if (success) TempData["Success"] = message;
        else TempData["Error"] = message;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(ServiceTickets));
    }

    /// <summary>
    /// POST: /Admin/CreateServiceQuote
    /// Kỹ thuật viên gửi báo giá chi phí sửa chữa / linh kiện cho khách duyệt
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateServiceQuote(int ticketId, string description, decimal amount, string? returnUrl = null)
    {
        var adminName = User.Identity?.Name ?? "Kỹ thuật viên";
        var (success, message) = await _warrantyService.CreateQuoteAsync(ticketId, description, amount, adminName);

        if (success) TempData["Success"] = message;
        else TempData["Error"] = message;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(ServiceTickets));
    }

    /// <summary>
    /// GET: /Admin/SoldDevices
    /// Quản lý Máy đã bán & Số Serial để phục vụ tra cứu và tiếp nhận bảo hành
    /// </summary>
    public async Task<IActionResult> SoldDevices(string? search = null)
    {
        var devices = await _warrantyService.GetAllSoldDevicesAsync(search);
        ViewBag.Search = search ?? "";
        return View(devices);
    }

    /// <summary>
    /// POST: /Admin/AssignOrderSerials
    /// Gán hoặc bổ sung số Serial cho các laptop trong đơn hàng đã giao
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignOrderSerials(int orderId, List<int> orderItemIds, List<string> serialLists, string? returnUrl = null)
    {
        var adminName = User.Identity?.Name ?? "Quản trị viên";
        var inputs = new List<OrderItemSerialsInput>();

        for (int i = 0; i < orderItemIds.Count; i++)
        {
            var rawSerials = (i < serialLists.Count ? serialLists[i] : "") ?? "";
            var serials = rawSerials
                .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            inputs.Add(new OrderItemSerialsInput
            {
                OrderItemId = orderItemIds[i],
                SerialNumbers = serials
            });
        }

        var (success, message) = await _warrantyService.AssignSerialsToOrderAsync(orderId, inputs, adminName);
        if (success) TempData["Success"] = message;
        else TempData["Error"] = message;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Orders));
    }

    /// <summary>
    /// GET: /Admin/ServiceCatalog
    /// Quản lý Danh mục dịch vụ Sửa chữa & Bảo hành
    /// </summary>
    public async Task<IActionResult> ServiceCatalog()
    {
        var items = await _warrantyService.GetCatalogAsync();
        return View(items);
    }

    /// <summary>
    /// POST: /Admin/ToggleServiceStatus
    /// Bật/tắt trạng thái nhận một hạng mục dịch vụ
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleServiceStatus(int id)
    {
        var s = await _db.ServiceCatalog.FindAsync(id);
        if (s != null)
        {
            s.IsActive = !s.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật trạng thái dịch vụ '{s.ServiceName}' thành {(s.IsActive ? "Đang nhận" : "Tạm ngưng")}.";
        }
        return RedirectToAction(nameof(ServiceCatalog));
    }
}
