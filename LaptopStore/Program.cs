using LaptopStore.Data;
using LaptopStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

// Tự động định vị ContentRootPath và WebRootPath kể cả khi chạy trực tiếp file .exe từ bin\Debug
var currentDir = Directory.GetCurrentDirectory();
var webRoot = Path.Combine(currentDir, "wwwroot");
var contentRoot = currentDir;

if (!Directory.Exists(webRoot))
{
    var baseWebRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
    if (Directory.Exists(baseWebRoot))
    {
        contentRoot = AppContext.BaseDirectory;
        webRoot = baseWebRoot;
    }
    else
    {
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var projectWebRoot = Path.Combine(projectDir, "wwwroot");
        if (Directory.Exists(projectWebRoot))
        {
            contentRoot = projectDir;
            webRoot = projectWebRoot;
        }
    }
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot,
    WebRootPath = webRoot
});

// =========================================================================
// 1. CẤU HÌNH CÁC DỊCH VỤ (DEPENDENCY INJECTION - DI SERVICES)
// =========================================================================

// 1.1. Kết nối Cơ sở dữ liệu Microsoft SQL Server qua Entity Framework Core
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=LaptopStoreDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

// Tự động khởi động và phân giải Named Pipe cho (localdb) (kế thừa cơ chế xử lý từ LapTrinhWeb - TvcLesson08Df)
if (connStr.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        using (var startProc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sqllocaldb", "start MSSQLLocalDB")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            startProc?.WaitForExit(3000);
        }

        using (var infoProc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sqllocaldb", "info MSSQLLocalDB")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            if (infoProc != null)
            {
                var output = infoProc.StandardOutput.ReadToEnd();
                infoProc.WaitForExit(3000);
                var match = System.Text.RegularExpressions.Regex.Match(output, @"np:\\\\\.\\pipe\\[^\r\n]+");
                if (match.Success)
                {
                    var pipe = match.Value.Trim();
                    connStr = System.Text.RegularExpressions.Regex.Replace(
                        connStr,
                        @"(Server|Data Source)\s*=\s*\(localdb\)\\[^;]+",
                        $"Server={pipe}",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
            }
        }
    }
    catch
    {
        // Giữ nguyên chuỗi kết nối mặc định nếu không chạy được sqllocaldb
    }
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(connStr, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(2),
            errorNumbersToAdd: null)));

// 1.2. Đăng ký các Service tầng nghiệp vụ (Clean MVC / Service Layer Pattern)
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// 1.3. Kích hoạt mô hình ASP.NET Core MVC (hỗ trợ cả Razor Views và Web API Controllers)
builder.Services.AddControllersWithViews();

// 1.4. Cấu hình Xác thực bằng Cookie cho Trang Quản trị Admin
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/AccessDenied";
        options.LogoutPath = "/Admin/Logout";
        options.Cookie.Name = "LaptopStore_Admin_Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// 1.5. Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(60);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});

// 1.6. Cấu hình chính sách CORS để React Frontend (port 3000) có thể gọi API kèm Credentials
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowReactApp", p =>
    {
        p.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000", "http://localhost:5173")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    });
});

var app = builder.Build();

// =========================================================================
// 2. KHỞI TẠO CƠ SỞ DỮ LIỆU & DỮ LIỆU MẪU BAN ĐẦU (DATABASE SEEDING)
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Tự động kiểm tra tạo bảng và nạp dữ liệu mẫu ban đầu một cách an toàn
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi tự động khởi tạo cơ sở dữ liệu.");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("=================================================================");
        Console.WriteLine("[LỖI KẾT NỐI CSDL SQL SERVER]");
        Console.WriteLine("Chi tiết lỗi: " + ex.Message);
        if (ex.InnerException != null)
        {
            Console.WriteLine("Nguyên nhân: " + ex.InnerException.Message);
        }
        Console.WriteLine("Vui lòng kiểm tra lại SQL Server (.\\SQLEXPRESS hoặc LocalDB) và chuỗi kết nối trong appsettings.json!");
        Console.WriteLine("=================================================================");
        Console.ResetColor();
    }
}

// =========================================================================
// 3. CẤU HÌNH ĐƯỜNG ỐNG XỬ LÝ YÊU CẦU HTTP (HTTP REQUEST PIPELINE / MIDDLEWARE)
// =========================================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Cho phép phục vụ file tĩnh (ảnh, css, js trong thư mục wwwroot)
app.UseStaticFiles();

// Bật cơ chế định tuyến URL
app.UseRouting();

// Kích hoạt CORS cho React Frontend
app.UseCors("AllowReactApp");

// Kích hoạt Session
app.UseSession();

// Kích hoạt Xác thực (Authentication) trước Phân quyền (Authorization)
app.UseAuthentication();
app.UseAuthorization();

// Định tuyến mặc định cho trang Quản trị Admin MVC (/Product/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

// Định tuyến cho các Web API Controllers (/api/products, /api/orders, /api/auth)
app.MapControllers();

// =========================================================================
// 4. TIỆN ÍCH TỰ ĐỘNG BẬT REACT FRONTEND (PORT 3000)
// =========================================================================
// Nếu đã được khởi động từ run.bat hoặc run.ps1 thì bỏ qua hoàn toàn để không mở thêm terminal thứ 2
if (Environment.GetEnvironmentVariable("LAUNCHED_BY_RUN_BAT") != "true")
{
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(2500);

            var frontendCandidates = new[]
            {
                Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "frontend")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "frontend")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "frontend")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "frontend"))
            };
            var frontendDir = frontendCandidates.FirstOrDefault(Directory.Exists);

            if (frontendDir != null)
            {
                using var tcpClient = new System.Net.Sockets.TcpClient();
                try
                {
                    // Kiểm tra xem cổng 3000 đã có React chạy chưa
                    await tcpClient.ConnectAsync("127.0.0.1", 3000);
                    return; // Đã chạy rồi thì không cần bật lại
                }
                catch
                {
                    // Cổng 3000 chưa có tiến trình nào, tiến hành chạy lệnh "npm run dev"
                }

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npm.cmd run dev",
                    WorkingDirectory = frontendDir,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized
                };
                System.Diagnostics.Process.Start(psi);
            }
        }
        catch { }
    });
}

// Tự động giải phóng cổng 5148 nếu có tiến trình LaptopStore cũ bị treo
try
{
    var currentProcId = System.Diagnostics.Process.GetCurrentProcess().Id;
    foreach (var proc in System.Diagnostics.Process.GetProcessesByName("LaptopStore"))
    {
        if (proc.Id != currentProcId)
        {
            try { proc.Kill(); proc.WaitForExit(1000); } catch { }
        }
    }
}
catch { }

// Khởi động Web Server Kestrel
try
{
    app.Run();
}
catch (System.IO.IOException ex) when (ex.Message.Contains("address already in use"))
{
    try
    {
        var currentProcId = System.Diagnostics.Process.GetCurrentProcess().Id;
        foreach (var proc in System.Diagnostics.Process.GetProcessesByName("LaptopStore"))
        {
            if (proc.Id != currentProcId)
            {
                try { proc.Kill(); proc.WaitForExit(1500); } catch { }
            }
        }
        app.Run();
    }
    catch
    {
        Console.WriteLine($"[Cảnh báo cổng 5148]: {ex.Message}");
    }
}