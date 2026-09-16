using LaptopStore.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Kết nối MySQL
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)), mySqlOptions =>
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

// Thêm MVC & API Controllers
builder.Services.AddControllersWithViews();

// Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(30);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});

// Cho phép CORS cho React frontend
builder.Services.AddCors(opt =>

{
    opt.AddPolicy("AllowReactApp", p =>
    {
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Tự động khởi tạo database & dữ liệu mẫu ban đầu
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi tự động khởi tạo cơ sở dữ liệu.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowReactApp");
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

app.MapControllers();

// Tự động khởi động React Vite Frontend khi bấm Run (F5) trong Visual Studio
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
                    await tcpClient.ConnectAsync("127.0.0.1", 3000);
                    // Port 3000 đã có tiến trình chạy, không cần khởi động lại
                    return;
                }
                catch
                {
                    // Port 3000 chưa bật, tiến hành khởi động npm run dev
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

app.Run();