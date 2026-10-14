using LaptopStore.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Data;

/// <summary>
/// Lớp hỗ trợ tự động khởi tạo cơ sở dữ liệu và nạp dữ liệu mẫu ban đầu (Seeding Data)
/// Giúp người chấm bài hoặc người clone code về không cần tự tạo bảng hay import thủ công
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Phương thức kiểm tra và khởi tạo Database
    /// 1. Gọi EnsureCreated() để tạo Database và các Bảng nếu chưa có
    /// 2. Nạp dữ liệu 100+ laptop từ file sql-init/init-db.sql nếu bảng còn trống
    /// </summary>
    public static void Initialize(AppDbContext context)
    {
        // Tự động tạo cơ sở dữ liệu MySQL và các bảng theo cấu hình EF Core nếu chưa tồn tại
        context.Database.EnsureCreated();

        // Nếu trong bảng Products đã có dữ liệu rồi thì bỏ qua, không nạp lại để tránh trùng lặp
        SeedProductImagesIfEmpty(context);
        SeedUsersIfEmpty(context);
        if (context.Products.Count() >= 50)
        {
            return; // Đã có đầy đủ dữ liệu
        }

        // Tự động nạp bộ 100 laptop từ sql-init/init-db.sql nếu có
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "sql-init", "init-db.sql"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "sql-init", "init-db.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "sql-init", "init-db.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "sql-init", "init-db.sql")
        };
        string? sqlFile = possiblePaths.FirstOrDefault(File.Exists);
        if (sqlFile != null)
        {
            try
            {
                context.Database.OpenConnection();
                var rawSql = File.ReadAllText(sqlFile);
                var statements = rawSql.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var stmt in statements)
                {
                    // Lọc bỏ từng dòng chú thích '--' và câu lệnh 'USE' bên trong mỗi block
                    var lines = stmt.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                                    .Select(l => l.Trim())
                                    .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("--") && !l.StartsWith("USE ", StringComparison.OrdinalIgnoreCase));

                    var cleanSql = string.Join(" ", lines).Trim();
                    if (string.IsNullOrWhiteSpace(cleanSql))
                    {
                        continue;
                    }

                    // Chuyển TRUNCATE TABLE thành DELETE FROM để tránh lỗi MySQL InnoDB Error 1701 với Foreign Key
                    if (cleanSql.StartsWith("TRUNCATE TABLE ", StringComparison.OrdinalIgnoreCase))
                    {
                        cleanSql = "DELETE FROM " + cleanSql.Substring("TRUNCATE TABLE ".Length);
                    }

                    try
                    {
                        context.Database.ExecuteSqlRaw(cleanSql);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DbInitializer] Lỗi khi thực thi SQL: {ex.Message}");
                    }
                }

                if (context.Products.Count() >= 50) return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbInitializer] Lỗi khi nạp file SQL: {ex.Message}");
            }
            finally
            {
                try { context.Database.CloseConnection(); } catch { }
            }
        }

        // 1. Thêm danh sách Thương hiệu (Brands)
        var brands = new Brand[]
        {
            new() { BrandName = "ASUS", LogoUrl = "/images/brands/asus.png", Description = "Thương hiệu laptop hàng đầu đến từ Đài Loan" },
            new() { BrandName = "Dell", LogoUrl = "/images/brands/dell.png", Description = "Hãng laptop Mỹ nổi tiếng về độ bền bỉ" },
            new() { BrandName = "Lenovo", LogoUrl = "/images/brands/lenovo.png", Description = "Đa dạng phân khúc với ThinkPad và Legion" },
            new() { BrandName = "Apple", LogoUrl = "/images/brands/apple.png", Description = "Hệ sinh thái MacBook với chip Apple Silicon" },
            new() { BrandName = "Acer", LogoUrl = "/images/brands/acer.png", Description = "Laptop gaming và văn phòng giá tốt" }
        };
        context.Brands.AddRange(brands);
        context.SaveChanges();

        // 2. Thêm danh sách Danh mục (Categories)
        var categories = new Category[]
        {
            new() { CategoryName = "Laptop gaming", Description = "Cấu hình mạnh mẽ, card rời, tản nhiệt tốt" },
            new() { CategoryName = "Laptop văn phòng", Description = "Gọn nhẹ, pin trâu, bàn phím gõ êm" },
            new() { CategoryName = "Laptop đồ họa", Description = "Màn hình chuẩn màu, chip hiệu năng cao" }
        };
        context.Categories.AddRange(categories);
        context.SaveChanges();

        // 3. Thêm danh sách Sản phẩm mẫu (Products & Specifications)
        var p1 = new Product
        {
            ProductName = "ASUS TUF Gaming F15 FX507ZC4",
            BrandId = brands[0].BrandId,
            CategoryId = categories[0].CategoryId,
            Price = 21990000,
            DiscountPrice = 18490000,
            StockQuantity = 25,
            ThumbnailUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=400&q=80",
            Description = "<p>Laptop gaming quốc dân phân khúc dưới 20 triệu với card rời RTX 3050 và màn hình 144Hz sắc nét.</p>",
            IsActive = true,
            Specification = new ProductSpecification
            {
                CPU = "Intel Core i5-12500H",
                RamGB = 16,
                StorageGB = 512,
                StorageType = "SSD NVMe PCIe",
                GPU = "NVIDIA GeForce RTX 3050 4GB",
                ScreenSizeInch = 15.6m,
                RefreshRateHz = 144,
                WeightKg = 2.20m,
                BatteryWh = 56,
                OS = "Windows 11 Home"
            }
        };

        var p2 = new Product
        {
            ProductName = "Dell Inspiron 15 3520",
            BrandId = brands[1].BrandId,
            CategoryId = categories[1].CategoryId,
            Price = 14500000,
            DiscountPrice = 12990000,
            StockQuantity = 40,
            ThumbnailUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=400&q=80",
            Description = "<p>Lựa chọn bền bỉ cho văn phòng, màn hình 120Hz mượt mà và bàn phím êm ái.</p>",
            IsActive = true,
            Specification = new ProductSpecification
            {
                CPU = "Intel Core i5-1235U",
                RamGB = 8,
                StorageGB = 512,
                StorageType = "SSD NVMe PCIe",
                GPU = "Intel Iris Xe Graphics",
                ScreenSizeInch = 15.6m,
                RefreshRateHz = 120,
                WeightKg = 1.65m,
                BatteryWh = 41,
                OS = "Windows 11 Home"
            }
        };

        var p3 = new Product
        {
            ProductName = "Lenovo Legion 5 16IRX9",
            BrandId = brands[2].BrandId,
            CategoryId = categories[0].CategoryId,
            Price = 35990000,
            DiscountPrice = 32490000,
            StockQuantity = 15,
            ThumbnailUrl = "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=400&q=80",
            Description = "<p>Hiệu năng gaming đỉnh cao với Intel Gen 14 và RTX 4060, màn hình 165Hz chuẩn màu.</p>",
            IsActive = true,
            Specification = new ProductSpecification
            {
                CPU = "Intel Core i7-14650HX",
                RamGB = 16,
                StorageGB = 512,
                StorageType = "SSD NVMe PCIe Gen4",
                GPU = "NVIDIA GeForce RTX 4060 8GB",
                ScreenSizeInch = 16.0m,
                RefreshRateHz = 165,
                WeightKg = 2.30m,
                BatteryWh = 80,
                OS = "Windows 11 Home"
            }
        };

        context.Products.AddRange(p1, p2, p3);
        context.SaveChanges();
    }

    public static void SeedProductImagesIfEmpty(AppDbContext context)
    {
        try
        {
            if (context.Products.Any() && !context.ProductImages.Any())
            {
                var allProds = context.Products.Include(p => p.Category).Include(p => p.Brand).ToList();
                var imgs = new List<ProductImage>();
                foreach (var p in allProds)
                {
                    if (!string.IsNullOrEmpty(p.ThumbnailUrl))
                    {
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = p.ThumbnailUrl, DisplayOrder = 1 });
                    }
                    bool isGaming = (p.Category?.CategoryName ?? "").ToLower().Contains("gaming");
                    bool isApple = (p.Brand?.BrandName ?? "").ToLower().Contains("apple");

                    if (isGaming)
                    {
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800&q=80", DisplayOrder = 2 });
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&q=80", DisplayOrder = 3 });
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800&q=80", DisplayOrder = 4 });
                    }
                    else if (isApple)
                    {
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&q=80", DisplayOrder = 2 });
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=800&q=80", DisplayOrder = 3 });
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=800&q=80", DisplayOrder = 4 });
                    }
                    else
                    {
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800&q=80", DisplayOrder = 2 });
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&q=80", DisplayOrder = 3 });
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=800&q=80", DisplayOrder = 4 });
                    }
                }

                if (imgs.Any())
                {
                    context.ProductImages.AddRange(imgs);
                    context.SaveChanges();
                    Console.WriteLine($"[DbInitializer] Added {imgs.Count} images into ProductImages table.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer] Error seeding ProductImages: {ex.Message}");
        }
    }


    /// <summary>
    /// Tự động nạp tài khoản mẫu (Admin & Khách hàng) nếu bảng Users còn trống
    /// Phục vụ kiểm thử chức năng Đăng nhập / Đăng ký phân quyền người dùng
    /// </summary>
    public static void SeedUsersIfEmpty(AppDbContext context)
    {
        try
        {
            // Đảm bảo bảng Users được tạo trong MySQL nếu CSDL đã có từ trước
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS `Users` (
                    `UserId` INT NOT NULL AUTO_INCREMENT,
                    `Username` VARCHAR(50) NOT NULL,
                    `Email` VARCHAR(100) NOT NULL,
                    `PasswordHash` VARCHAR(255) NOT NULL,
                    `FullName` VARCHAR(100) NULL,
                    `Role` VARCHAR(20) NOT NULL DEFAULT 'Customer',
                    `GoogleId` VARCHAR(100) NULL,
                    `AvatarUrl` VARCHAR(255) NULL,
                    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (`UserId`),
                    UNIQUE KEY `IX_Users_Username` (`Username`),
                    UNIQUE KEY `IX_Users_Email` (`Email`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            if (!context.Users.Any())
            {
                var admin = new User
                {
                    Username = "admin",
                    Email = "admin@pcstore.vn",
                    FullName = "Quản Trị Viên",
                    PasswordHash = LaptopStore.Helpers.PasswordHelper.HashPassword("Admin@123"),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                var customer = new User
                {
                    Username = "khachhang",
                    Email = "khachhang@gmail.com",
                    FullName = "Nguyễn Văn A",
                    PasswordHash = LaptopStore.Helpers.PasswordHelper.HashPassword("User@123"),
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.AddRange(admin, customer);
                context.SaveChanges();
                Console.WriteLine("[DbInitializer] Đã nạp thành công tài khoản mẫu: admin@pcstore.vn và khachhang@gmail.com");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DbInitializer] Lỗi SeedUsersIfEmpty: " + ex.Message);
        }
    }
}
