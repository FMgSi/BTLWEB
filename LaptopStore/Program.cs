using LaptopStore.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. CẤU HÌNH CÁC DỊCH VỤ (DEPENDENCY INJECTION - DI SERVICES)
// =========================================================================

// 1.1. Kết nối Cơ sở dữ liệu MySQL qua Entity Framework Core
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)), mySqlOptions =>
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

// 1.2. Kích hoạt mô hình ASP.NET Core MVC (hỗ trợ cả Razor Views và Web API Controllers)
builder.Services.AddControllersWithViews();

// 1.3. Cấu hình Session (dùng để lưu thông báo tạm thời sau khi Thêm/Sửa/Xóa sản phẩm)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(30);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});

// 1.4. Cấu hình chính sách CORS (Cross-Origin Resource Sharing) để React Frontend (port 3000) có thể gọi API
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowReactApp", p =>
    {
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
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
        // Tự động kiểm tra tạo bảng và nạp 100+ laptop mẫu nếu database còn trống
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi tự động khởi tạo cơ sở dữ liệu.");
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

// Kích hoạt xác thực và phân quyền
app.UseAuthorization();

// Định tuyến mặc định cho trang Quản trị Admin MVC (/Product/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

// Định tuyến cho các Web API Controllers (/api/products)
app.MapControllers();

// =========================================================================
// 4. TIỆN ÍCH TỰ ĐỘNG BẬT REACT FRONTEND KHI CHẠY TRÊN VISUAL STUDIO (F5)
// =========================================================================
if (app.Environment.IsDevelopment())
{
    _ = Task.Run(async () =>
    {
        try
        {
            var frontendDir = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "frontend"));
            if (Directory.Exists(frontendDir))
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

// Khởi động Web Server Kestrel
app.Run();