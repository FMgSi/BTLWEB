using LaptopStore.Data;
using LaptopStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

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

var configuredConnStr = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=.\\SQLEXPRESS;Database=LaptopStoreDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

var connStr = AutoDetectWorkingSqlServer(configuredConnStr);

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
        
    }
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(connStr, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(2),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInstallmentService, InstallmentService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IWarrantyService, WarrantyService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();

builder.Services.AddControllersWithViews();

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

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(60);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});

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

if (args.Contains("--reset-db", StringComparer.OrdinalIgnoreCase) || args.Contains("--recreate-db", StringComparer.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[*] Phát hiện cờ lệnh --reset-db: Đang tiến hành xóa sạch và tạo mới lại toàn bộ CSDL...");
            Console.ResetColor();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            DbInitializer.Initialize(context);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[✓] Đã xóa và tái tạo mới CSDL LaptopStoreDb thành công cùng toàn bộ dữ liệu mẫu!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[LỖI --reset-db]: " + ex.Message);
            Console.ResetColor();
        }
    }
}

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowReactApp");

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

app.MapControllers();

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
                    
                    await tcpClient.ConnectAsync("127.0.0.1", 3000);
                    return; 
                }
                catch
                {
                    
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

static string AutoDetectWorkingSqlServer(string initialConnStr)
{
    
    if (CanConnectToSqlServer(initialConnStr))
    {
        return initialConnStr;
    }

    var candidateServers = new[]
    {
        "Server=.\\SQLEXPRESS;Database=LaptopStoreDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;",
        "Server=(localdb)\\MSSQLLocalDB;Database=LaptopStoreDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;",
        "Server=localhost;Database=LaptopStoreDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;",
        "Server=.;Database=LaptopStoreDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
    };

    foreach (var candidate in candidateServers)
    {
        if (candidate.Equals(initialConnStr, StringComparison.OrdinalIgnoreCase)) continue;

        if (candidate.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sqllocaldb", "start MSSQLLocalDB")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                p?.WaitForExit(2000);
            }
            catch { }
        }

        if (CanConnectToSqlServer(candidate))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[*] Tự động phát hiện và kết nối thành công tới SQL Server: {GetServerName(candidate)}");
            Console.ResetColor();
            return candidate;
        }
    }

    return initialConnStr;
}

static bool CanConnectToSqlServer(string connectionString)
{
    try
    {
        var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = 2,
            InitialCatalog = "master" 
        };
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(csb.ConnectionString);
        conn.Open();
        return true;
    }
    catch
    {
        return false;
    }
}

static string GetServerName(string connectionString)
{
    try
    {
        var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        return csb.DataSource;
    }
    catch
    {
        return connectionString;
    }
}
