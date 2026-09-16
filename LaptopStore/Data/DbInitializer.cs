using LaptopStore.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Tự động tạo cơ sở dữ liệu và bảng nếu chưa tồn tại
        context.Database.EnsureCreated();

        // Kiểm tra xem đã có đủ dữ liệu hay chưa
        if (context.Products.Count() >= 50)
        {
            return; // Đã có dữ liệu 100 laptop
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
                var rawSql = File.ReadAllText(sqlFile);
                var statements = rawSql.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var stmt in statements)
                {
                    var trimmed = stmt.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("--") && !trimmed.StartsWith("USE"))
                    {
                        try { context.Database.ExecuteSqlRaw(trimmed); } catch { }
                    }
                }
                if (context.Products.Any()) return;
            }
            catch { }
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
}
