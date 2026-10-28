using LaptopStore.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Data;

/// <summary>
/// Lớp hỗ trợ tự động khởi tạo cơ sở dữ liệu và nạp dữ liệu mẫu ban đầu (Seeding Data an toàn)
/// Đảm bảo KHÔNG BAO GIỜ xóa dữ liệu thật đã nhập khi khởi động lại ứng dụng
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Phương thức kiểm tra và khởi tạo Database an toàn
    /// </summary>
    public static void Initialize(AppDbContext context)
    {
        // 1. Tự động tạo cơ sở dữ liệu SQL Server và toàn bộ 10 bảng theo metadata EF Core nếu chưa tồn tại
        context.Database.EnsureCreated();

        // 2. Nạp tài khoản mẫu nếu bảng Users còn trống
        SeedUsersIfEmpty(context);

        // 3. KIỂM TRA SẢN PHẨM: Nếu đã có bất kỳ sản phẩm nào -> DỪNG NGAY, không nạp đè và không chạy script xóa!
        if (context.Products.Any())
        {
            SeedProductImagesIfEmpty(context);
            SeedOrdersIfEmpty(context);
            return;
        }

        // 4. Nếu database HOÀN TOÀN TRỐNG (lần đầu setup đồ án), nạp dữ liệu từ file sql-init/init-db.sql (đã lọc bỏ DELETE/TRUNCATE)
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

                // Tách theo từ khóa GO (chuẩn SQL Server) hoặc dấu chấm phẩy
                var batches = System.Text.RegularExpressions.Regex.Split(rawSql, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                foreach (var batch in batches)
                {
                    var cleanBatch = batch.Trim();
                    if (string.IsNullOrWhiteSpace(cleanBatch))
                        continue;

                    // BẢO VỆ DỮ LIỆU: Bỏ qua hoàn toàn các câu lệnh DELETE / TRUNCATE / DROP
                    if (cleanBatch.StartsWith("DELETE FROM", StringComparison.OrdinalIgnoreCase) ||
                        cleanBatch.StartsWith("TRUNCATE TABLE", StringComparison.OrdinalIgnoreCase) ||
                        cleanBatch.StartsWith("DROP TABLE", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        context.Database.ExecuteSqlRaw(cleanBatch);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DbInitializer] Lỗi khi thực thi khối SQL: {ex.Message}");
                    }
                }

                if (context.Products.Any())
                {
                    SeedProductImagesIfEmpty(context);
                    SeedOrdersIfEmpty(context);
                    return;
                }
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

        // 5. Fallback Seed nếu không tìm thấy file SQL
        SeedFallbackProducts(context);
        SeedProductImagesIfEmpty(context);
        SeedOrdersIfEmpty(context);
    }



    private static void SeedFallbackProducts(AppDbContext context)
    {
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

        var categories = new Category[]
        {
            new() { CategoryName = "Laptop gaming", Description = "Dành riêng cho game thủ với GPU rời mạnh mẽ và tản nhiệt tối ưu" },
            new() { CategoryName = "Laptop văn phòng", Description = "Thiết kế mỏng nhẹ, pin trâu phục vụ công việc và học tập" },
            new() { CategoryName = "Laptop đồ họa", Description = "Màn hình chuẩn màu cao, CPU/GPU hiệu năng cho sáng tạo nội dung" }
        };
        context.Categories.AddRange(categories);
        context.SaveChanges();

        var products = new List<Product>
        {
            new()
            {
                ProductName = "ASUS ROG Strix G16 G614JV",
                BrandId = brands[0].BrandId,
                CategoryId = categories[0].CategoryId,
                Price = 39990000m,
                DiscountPrice = 36990000m,
                StockQuantity = 12,
                ThumbnailUrl = "/images/products/asus-rog-strix-g16.png",
                Description = "Laptop Gaming đỉnh cao với Intel Core i7-13650HX và RTX 4060 8GB, màn hình 165Hz.",
                IsActive = true,
                Specification = new ProductSpecification
                {
                    CPU = "Intel Core i7-13650HX",
                    RamGB = 16,
                    StorageGB = 512,
                    StorageType = "SSD",
                    GPU = "NVIDIA RTX 4060 8GB",
                    ScreenSizeInch = 16.0m,
                    RefreshRateHz = 165,
                    WeightKg = 2.5m,
                    BatteryWh = 90,
                    OS = "Windows 11 Home"
                }
            },
            new()
            {
                ProductName = "MacBook Air 13 M3 2024",
                BrandId = brands[3].BrandId,
                CategoryId = categories[1].CategoryId,
                Price = 27990000m,
                DiscountPrice = 26490000m,
                StockQuantity = 20,
                ThumbnailUrl = "/images/products/macbook-air-m3.png",
                Description = "Siêu mỏng nhẹ với chip Apple M3 mạnh mẽ, thời lượng pin lên đến 18 giờ.",
                IsActive = true,
                Specification = new ProductSpecification
                {
                    CPU = "Apple M3 8-core",
                    RamGB = 16,
                    StorageGB = 512,
                    StorageType = "SSD",
                    GPU = "10-core GPU",
                    ScreenSizeInch = 13.6m,
                    RefreshRateHz = 60,
                    WeightKg = 1.24m,
                    BatteryWh = 52,
                    OS = "macOS Sonoma"
                }
            },
            new()
            {
                ProductName = "Dell Inspiron 16 5630",
                BrandId = brands[1].BrandId,
                CategoryId = categories[1].CategoryId,
                Price = 21990000m,
                DiscountPrice = 19990000m,
                StockQuantity = 8,
                ThumbnailUrl = "/images/products/dell-inspiron-16.png",
                Description = "Laptop văn phòng thanh lịch, màn hình lớn 16 inch sắc nét, vi xử lý Intel Gen 13.",
                IsActive = true,
                Specification = new ProductSpecification
                {
                    CPU = "Intel Core i5-1335U",
                    RamGB = 16,
                    StorageGB = 512,
                    StorageType = "SSD",
                    GPU = "Intel Iris Xe",
                    ScreenSizeInch = 16.0m,
                    RefreshRateHz = 60,
                    WeightKg = 1.85m,
                    BatteryWh = 54,
                    OS = "Windows 11 Home"
                }
            }
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }

    public static void SeedProductImagesIfEmpty(AppDbContext context)
    {
        try
        {
            if (!context.ProductImages.Any() && context.Products.Any())
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
                    }
                    else if (isApple)
                    {
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&q=80", DisplayOrder = 2 });
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=800&q=80", DisplayOrder = 3 });
                    }
                    else
                    {
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800&q=80", DisplayOrder = 2 });
                        imgs.Add(new ProductImage { ProductId = p.ProductId, ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&q=80", DisplayOrder = 3 });
                    }
                }

                if (imgs.Any())
                {
                    context.ProductImages.AddRange(imgs);
                    context.SaveChanges();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer] Error seeding ProductImages: {ex.Message}");
        }
    }

    public static void SeedUsersIfEmpty(AppDbContext context)
    {
        try
        {
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
                Console.WriteLine("[DbInitializer] Đã nạp thành công tài khoản mẫu: admin@pcstore.vn (Admin@123) và khachhang@gmail.com (User@123)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DbInitializer] Lỗi SeedUsersIfEmpty: " + ex.Message);
        }
    }

    public static void SeedOrdersIfEmpty(AppDbContext context)
    {
        try
        {
            if (!context.Orders.Any() && context.Products.Any())
            {
                var prod1 = context.Products.FirstOrDefault();
                var prod2 = context.Products.Skip(1).FirstOrDefault() ?? prod1;

                if (prod1 == null) return;

                var price1 = prod1.DiscountPrice ?? prod1.Price;
                var price2 = prod2 != null ? (prod2.DiscountPrice ?? prod2.Price) : price1;

                var order1 = new Order
                {
                    OrderCode = "PC20261028-1001",
                    CustomerName = "Nguyễn Hoàng Minh",
                    PhoneNumber = "0901234567",
                    Email = "hoangminh@gmail.com",
                    ShippingAddress = "123 Cầu Giấy, Hà Nội",
                    Notes = "Giao giờ hành chính",
                    SubTotal = price1,
                    DiscountAmount = 0,
                    TotalAmount = price1,
                    PaymentMethod = "VietQR",
                    PaymentStatus = "Đã thanh toán",
                    OrderStatus = "Đã xác nhận",
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    OrderItems = new List<OrderItem>
                    {
                        new()
                        {
                            ProductId = prod1.ProductId,
                            ProductName = prod1.ProductName,
                            SelectedSpecs = "Cấu hình chuẩn",
                            UnitPrice = price1,
                            Quantity = 1,
                            TotalPrice = price1,
                            ThumbnailUrl = prod1.ThumbnailUrl
                        }
                    },
                    Payments = new List<Payment>
                    {
                        new()
                        {
                            PaymentMethod = "VietQR",
                            Amount = price1,
                            TransactionReference = "MB9988221",
                            PaymentStatus = "Thành công",
                            PaymentDate = DateTime.UtcNow.AddHours(-3)
                        }
                    },
                    StatusHistories = new List<OrderStatusHistory>
                    {
                        new() { Status = "Đang xử lý", ChangedAt = DateTime.UtcNow.AddHours(-3), Note = "Khách đặt đơn thành công" },
                        new() { Status = "Đã xác nhận", ChangedAt = DateTime.UtcNow.AddHours(-2), ChangedBy = "admin", Note = "Đã xác nhận tiền về qua VietQR" }
                    }
                };

                var order2 = new Order
                {
                    OrderCode = "PC20261028-1002",
                    CustomerName = "Trần Thu Hà",
                    PhoneNumber = "0987654321",
                    Email = "thuha@gmail.com",
                    ShippingAddress = "456 Lê Duẩn, TP.HCM",
                    Notes = "Gọi trước khi giao 15 phút",
                    SubTotal = price2,
                    DiscountAmount = 0,
                    TotalAmount = price2,
                    PaymentMethod = "COD",
                    PaymentStatus = "Chờ thanh toán",
                    OrderStatus = "Đang xử lý",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    OrderItems = new List<OrderItem>
                    {
                        new()
                        {
                            ProductId = prod2!.ProductId,
                            ProductName = prod2.ProductName,
                            SelectedSpecs = "Cấu hình chuẩn",
                            UnitPrice = price2,
                            Quantity = 1,
                            TotalPrice = price2,
                            ThumbnailUrl = prod2.ThumbnailUrl
                        }
                    },
                    Payments = new List<Payment>
                    {
                        new()
                        {
                            PaymentMethod = "COD",
                            Amount = price2,
                            PaymentStatus = "Chờ thanh toán"
                        }
                    },
                    StatusHistories = new List<OrderStatusHistory>
                    {
                        new() { Status = "Đang xử lý", ChangedAt = DateTime.UtcNow.AddHours(-1), Note = "Đơn hàng mới tạo (COD)" }
                    }
                };

                context.Orders.AddRange(order1, order2);
                context.SaveChanges();
                Console.WriteLine("[DbInitializer] Đã nạp thành công 2 đơn hàng mẫu ban đầu.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DbInitializer] Lỗi SeedOrdersIfEmpty: " + ex.Message);
        }
    }
}
