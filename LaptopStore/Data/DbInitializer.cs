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

        // Nâng cấp an toàn các cột mới nếu đang chạy trên CSDL SQL Server cũ
        if (context.Database.IsRelational())
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'IsActive')
                        ALTER TABLE Users ADD IsActive BIT NOT NULL DEFAULT 1;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PhoneNumber')
                        ALTER TABLE Users ADD PhoneNumber NVARCHAR(20) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'IdempotencyKey')
                        ALTER TABLE Orders ADD IdempotencyKey NVARCHAR(100) NULL;

                    SET ANSI_NULLS ON;
                    SET QUOTED_IDENTIFIER ON;

                    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_IdempotencyKey' AND is_unique = 0 AND object_id = OBJECT_ID('Orders'))
                        DROP INDEX IX_Orders_IdempotencyKey ON Orders;

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_IdempotencyKey' AND object_id = OBJECT_ID('Orders'))
                        CREATE UNIQUE NONCLUSTERED INDEX IX_Orders_IdempotencyKey ON Orders (IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;

                    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Products_Price')
                        ALTER TABLE Products ADD CONSTRAINT CK_Products_Price CHECK (Price >= 0);
                    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Products_StockQuantity')
                        ALTER TABLE Products ADD CONSTRAINT CK_Products_StockQuantity CHECK (StockQuantity >= 0);
                    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_OrderItems_Quantity')
                        ALTER TABLE OrderItems ADD CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0);
                    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_OrderItems_UnitPrice')
                        ALTER TABLE OrderItems ADD CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0);

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'PurchaseMode')
                        ALTER TABLE Orders ADD PurchaseMode NVARCHAR(50) NOT NULL DEFAULT 'Full';
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'PaymentType')
                        ALTER TABLE Payments ADD PaymentType NVARCHAR(50) NOT NULL DEFAULT 'Full';
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'ScheduleId')
                        ALTER TABLE Payments ADD ScheduleId INT NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'ConfirmedBy')
                        ALTER TABLE Payments ADD ConfirmedBy NVARCHAR(100) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderItems') AND name = 'VariantId')
                        ALTER TABLE OrderItems ADD VariantId INT NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderItems') AND name = 'ColorName')
                        ALTER TABLE OrderItems ADD ColorName NVARCHAR(100) NULL;

                    -- 1. Bảng danh mục màu sắc máy tính (Colors)
                    IF OBJECT_ID('dbo.Colors', 'U') IS NULL
                    BEGIN
                        CREATE TABLE Colors (
                            ColorId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            Name NVARCHAR(100) NOT NULL,
                            HexCode NVARCHAR(20) NOT NULL,
                            IsActive BIT NOT NULL DEFAULT 1,
                            CONSTRAINT IX_Colors_Name UNIQUE (Name)
                        );
                    END

                    IF OBJECT_ID('dbo.ProductVariants', 'U') IS NULL
                    BEGIN
                        CREATE TABLE ProductVariants (
                            VariantId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            ProductId INT NOT NULL,
                            ColorId INT NOT NULL,
                            RamGB INT NOT NULL,
                            StorageGB INT NOT NULL,
                            SKU NVARCHAR(100) NOT NULL,
                            StockQuantity INT NOT NULL DEFAULT 0,
                            Price DECIMAL(18,2) NULL,
                            DiscountPrice DECIMAL(18,2) NULL,
                            ImageUrl NVARCHAR(500) NULL,
                            IsActive BIT NOT NULL DEFAULT 1,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT IX_ProductVariants_SKU UNIQUE (SKU),
                            CONSTRAINT IX_ProductVariants_UniqueCombo UNIQUE (ProductId, ColorId, RamGB, StorageGB),
                            CONSTRAINT FK_ProductVariants_Products_ProductId FOREIGN KEY (ProductId) REFERENCES Products (ProductId) ON DELETE CASCADE,
                            CONSTRAINT FK_ProductVariants_Colors_ColorId FOREIGN KEY (ColorId) REFERENCES Colors (ColorId)
                        );
                    END
                    ELSE
                    BEGIN
                        -- Nâng cấp an toàn cho bảng ProductVariants nếu CSDL đã được tạo trước đó
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductVariants') AND name = 'CreatedAt')
                        BEGIN
                            ALTER TABLE ProductVariants ADD CreatedAt DATETIME NOT NULL DEFAULT GETDATE();
                        END

                        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductVariants') AND name = 'Price' AND is_nullable = 0)
                        BEGIN
                            DECLARE @pvPriceConstraint NVARCHAR(200);
                            SELECT @pvPriceConstraint = d.name 
                            FROM sys.default_constraints d 
                            JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
                            WHERE d.parent_object_id = OBJECT_ID('ProductVariants') AND c.name = 'Price';
                            
                            IF @pvPriceConstraint IS NOT NULL
                                EXEC('ALTER TABLE ProductVariants DROP CONSTRAINT ' + @pvPriceConstraint);

                            ALTER TABLE ProductVariants ALTER COLUMN Price DECIMAL(18,2) NULL;
                        END

                        UPDATE ProductVariants SET Price = NULL WHERE Price = 0;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_OrderItems_ProductVariants_VariantId')
                    BEGIN
                        ALTER TABLE OrderItems ADD CONSTRAINT FK_OrderItems_ProductVariants_VariantId 
                            FOREIGN KEY (VariantId) REFERENCES ProductVariants (VariantId) ON DELETE SET NULL;
                    END

                    IF OBJECT_ID('dbo.InstallmentContracts', 'U') IS NULL
                    BEGIN
                        CREATE TABLE InstallmentContracts (
                            ContractId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            ContractCode NVARCHAR(50) NOT NULL,
                            OrderId INT NOT NULL,
                            TermMonths INT NOT NULL,
                            PrepayPercent DECIMAL(5,2) NOT NULL DEFAULT 30.00,
                            PrepayAmount DECIMAL(18,2) NOT NULL,
                            TotalFinancedAmount DECIMAL(18,2) NOT NULL,
                            InterestRate DECIMAL(5,2) NOT NULL DEFAULT 0.00,
                            MonthlyInstallmentAmount DECIMAL(18,2) NOT NULL,
                            Status NVARCHAR(50) NOT NULL DEFAULT N'Chờ trả trước',
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                            ActivatedAt DATETIME NULL,
                            CompletedAt DATETIME NULL,
                            Notes NVARCHAR(500) NULL,
                            CONSTRAINT IX_InstallmentContracts_ContractCode UNIQUE (ContractCode),
                            CONSTRAINT IX_InstallmentContracts_OrderId UNIQUE (OrderId),
                            CONSTRAINT FK_InstallmentContracts_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES Orders (OrderId) ON DELETE CASCADE
                        );
                    END

                    IF OBJECT_ID('dbo.InstallmentSchedules', 'U') IS NULL
                    BEGIN
                        CREATE TABLE InstallmentSchedules (
                            ScheduleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            ContractId INT NOT NULL,
                            PeriodNumber INT NOT NULL,
                            DueDate DATETIME NOT NULL,
                            Amount DECIMAL(18,2) NOT NULL,
                            PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                            Status NVARCHAR(50) NOT NULL DEFAULT N'Chưa thanh toán',
                            PaidAt DATETIME NULL,
                            PaymentId INT NULL,
                            CollectedBy NVARCHAR(100) NULL,
                            CONSTRAINT IX_InstallmentSchedules_Contract_Period UNIQUE (ContractId, PeriodNumber),
                            CONSTRAINT FK_InstallmentSchedules_Contracts_ContractId FOREIGN KEY (ContractId) REFERENCES InstallmentContracts (ContractId) ON DELETE CASCADE,
                            CONSTRAINT FK_InstallmentSchedules_Payments_PaymentId FOREIGN KEY (PaymentId) REFERENCES Payments (PaymentId) ON DELETE NO ACTION
                        );
                    END

                    IF OBJECT_ID('dbo.InstallmentSchedules', 'U') IS NOT NULL AND NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Payments_InstallmentSchedules_ScheduleId')
                    BEGIN
                        ALTER TABLE Payments ADD CONSTRAINT FK_Payments_InstallmentSchedules_ScheduleId 
                            FOREIGN KEY (ScheduleId) REFERENCES InstallmentSchedules (ScheduleId) ON DELETE NO ACTION;
                    END

                    IF OBJECT_ID('dbo.SoldDevices', 'U') IS NULL
                    BEGIN
                        CREATE TABLE SoldDevices (
                            DeviceId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            OrderItemId INT NOT NULL,
                            SerialNumber NVARCHAR(100) NOT NULL,
                            DeliveredAt DATETIME NOT NULL DEFAULT GETDATE(),
                            WarrantyStartDate DATETIME NOT NULL DEFAULT GETDATE(),
                            WarrantyEndDate DATETIME NOT NULL,
                            WarrantyMonths INT NOT NULL DEFAULT 12,
                            Notes NVARCHAR(500) NULL,
                            CONSTRAINT IX_SoldDevices_SerialNumber UNIQUE (SerialNumber),
                            CONSTRAINT FK_SoldDevices_OrderItems_OrderItemId FOREIGN KEY (OrderItemId) REFERENCES OrderItems (OrderItemId) ON DELETE CASCADE,
                            CONSTRAINT CK_SoldDevices_WarrantyDate CHECK (WarrantyEndDate >= WarrantyStartDate)
                        );
                    END

                    IF OBJECT_ID('dbo.ServiceCatalog', 'U') IS NULL
                    BEGIN
                        CREATE TABLE ServiceCatalog (
                            ServiceId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            ServiceCode NVARCHAR(50) NOT NULL,
                            ServiceName NVARCHAR(200) NOT NULL,
                            ServiceType NVARCHAR(50) NOT NULL,
                            ApplicableConditions NVARCHAR(500) NULL,
                            EstimatedCost DECIMAL(18,2) NOT NULL DEFAULT 0,
                            EstimatedTime NVARCHAR(100) NULL,
                            ServiceWarrantyMonths INT NOT NULL DEFAULT 0,
                            IsActive BIT NOT NULL DEFAULT 1,
                            CONSTRAINT IX_ServiceCatalog_ServiceCode UNIQUE (ServiceCode),
                            CONSTRAINT CK_ServiceCatalog_EstimatedCost CHECK (EstimatedCost >= 0)
                        );
                    END

                    IF OBJECT_ID('dbo.ServiceTickets', 'U') IS NULL
                    BEGIN
                        CREATE TABLE ServiceTickets (
                            TicketId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            TicketCode NVARCHAR(50) NOT NULL,
                            TicketType NVARCHAR(50) NOT NULL,
                            ServiceId INT NULL,
                            SoldDeviceId INT NULL,
                            UserId INT NULL,
                            CustomerName NVARCHAR(100) NOT NULL,
                            PhoneNumber NVARCHAR(20) NOT NULL,
                            Email NVARCHAR(100) NULL,
                            Address NVARCHAR(255) NULL,
                            ExternalDeviceModel NVARCHAR(200) NULL,
                            ExternalSerialNumber NVARCHAR(100) NULL,
                            FaultDescription NVARCHAR(MAX) NOT NULL,
                            PhysicalCondition NVARCHAR(500) NULL,
                            Accessories NVARCHAR(255) NULL,
                            Status NVARCHAR(50) NOT NULL DEFAULT N'Chờ tiếp nhận',
                            RejectReason NVARCHAR(500) NULL,
                            AssignedStaff NVARCHAR(100) NULL,
                            FinalCost DECIMAL(18,2) NOT NULL DEFAULT 0,
                            ServiceWarrantyUntil DATETIME NULL,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                            ReceivedAt DATETIME NULL,
                            CompletedAt DATETIME NULL,
                            ReturnedAt DATETIME NULL,
                            Notes NVARCHAR(500) NULL,
                            CONSTRAINT IX_ServiceTickets_TicketCode UNIQUE (TicketCode),
                            CONSTRAINT FK_ServiceTickets_ServiceCatalog_ServiceId FOREIGN KEY (ServiceId) REFERENCES ServiceCatalog (ServiceId) ON DELETE SET NULL,
                            CONSTRAINT FK_ServiceTickets_SoldDevices_SoldDeviceId FOREIGN KEY (SoldDeviceId) REFERENCES SoldDevices (DeviceId) ON DELETE SET NULL,
                            CONSTRAINT FK_ServiceTickets_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (UserId) ON DELETE SET NULL,
                            CONSTRAINT CK_ServiceTickets_FinalCost CHECK (FinalCost >= 0)
                        );
                    END

                    IF OBJECT_ID('dbo.ServiceQuotes', 'U') IS NULL
                    BEGIN
                        CREATE TABLE ServiceQuotes (
                            QuoteId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            TicketId INT NOT NULL,
                            Description NVARCHAR(500) NOT NULL,
                            Amount DECIMAL(18,2) NOT NULL,
                            CustomerDecision NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt',
                            DecisionAt DATETIME NULL,
                            Notes NVARCHAR(500) NULL,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT FK_ServiceQuotes_ServiceTickets_TicketId FOREIGN KEY (TicketId) REFERENCES ServiceTickets (TicketId) ON DELETE CASCADE,
                            CONSTRAINT CK_ServiceQuotes_Amount CHECK (Amount >= 0)
                        );
                    END

                    IF OBJECT_ID('dbo.ServiceTicketHistories', 'U') IS NULL
                    BEGIN
                        CREATE TABLE ServiceTicketHistories (
                            HistoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            TicketId INT NOT NULL,
                            PreviousStatus NVARCHAR(50) NULL,
                            NewStatus NVARCHAR(50) NOT NULL,
                            ActionBy NVARCHAR(100) NOT NULL,
                            ActionAt DATETIME NOT NULL DEFAULT GETDATE(),
                            Notes NVARCHAR(500) NULL,
                            CONSTRAINT FK_ServiceTicketHistories_ServiceTickets_TicketId FOREIGN KEY (TicketId) REFERENCES ServiceTickets (TicketId) ON DELETE CASCADE
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'DiscountCode')
                    BEGIN
                        ALTER TABLE Orders ADD DiscountCode NVARCHAR(50) NULL;
                    END

                    IF OBJECT_ID('dbo.Discounts', 'U') IS NULL
                    BEGIN
                        CREATE TABLE Discounts (
                            DiscountId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            Code NVARCHAR(50) NOT NULL,
                            Title NVARCHAR(200) NOT NULL,
                            Description NVARCHAR(500) NULL,
                            DiscountType NVARCHAR(20) NOT NULL DEFAULT 'Percentage',
                            DiscountValue DECIMAL(18,2) NOT NULL,
                            MaxDiscountAmount DECIMAL(18,2) NULL,
                            MinOrderAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                            StartDate DATETIME NOT NULL DEFAULT GETDATE(),
                            EndDate DATETIME NULL,
                            UsageLimit INT NULL,
                            UsedCount INT NOT NULL DEFAULT 0,
                            ApplicableScope NVARCHAR(50) NOT NULL DEFAULT 'All',
                            IsActive BIT NOT NULL DEFAULT 1,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT IX_Discounts_Code UNIQUE (Code)
                        );
                    END
                ");

                // Bước 2: Nâng cấp an toàn cho bảng ProductImages (thêm cột ColorId trong batch độc lập)
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductImages') AND name = 'ColorId')
                    BEGIN
                        ALTER TABLE ProductImages ADD ColorId INT NULL;
                    END
                ");

                // Bước 3: Ràng buộc khóa ngoại và Index lọc ColorId với SET QUOTED_IDENTIFIER ON
                context.Database.ExecuteSqlRaw(@"
                    SET QUOTED_IDENTIFIER ON;
                    SET ANSI_NULLS ON;

                    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ProductImages_Colors_ColorId')
                    BEGIN
                        ALTER TABLE ProductImages ADD CONSTRAINT FK_ProductImages_Colors_ColorId FOREIGN KEY (ColorId) REFERENCES Colors (ColorId) ON DELETE SET NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductImages_Product_Color_Slot' AND object_id = OBJECT_ID('ProductImages'))
                    BEGIN
                        CREATE UNIQUE NONCLUSTERED INDEX IX_ProductImages_Product_Color_Slot ON ProductImages (ProductId, ColorId, DisplayOrder) WHERE ColorId IS NOT NULL;
                    END
                ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbInitializer] Cảnh báo khởi tạo schema CSDL: {ex.Message}");
            }
        }

        // 2. Nạp tài khoản mẫu nếu bảng Users còn trống
        SeedUsersIfEmpty(context);

        // 3. KIỂM TRA SẢN PHẨM: Nếu đã có bất kỳ sản phẩm nào -> DỪNG NGAY, không nạp đè và không chạy script xóa!
        if (context.Products.Any())
        {
            SeedProductImagesIfEmpty(context);
            SeedColorsIfEmpty(context);
            SeedVariantsIfEmpty(context);
            SeedOrdersIfEmpty(context);
            SeedServiceCatalogIfEmpty(context);
            SeedSampleSoldDevicesIfEmpty(context);
            SeedDiscountsIfEmpty(context);
            SyncUserPhoneAndLinkOrders(context);
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
                    SeedColorsIfEmpty(context);
                    SeedVariantsIfEmpty(context);
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
        SeedColorsIfEmpty(context);
        SeedVariantsIfEmpty(context);
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

            // Đồng bộ an toàn ảnh biến thể vào bảng ProductImages (Ô 1 - Ảnh đại diện của từng màu)
            var variantsWithImage = context.ProductVariants
                .AsNoTracking()
                .Where(v => !string.IsNullOrEmpty(v.ImageUrl) && v.ColorId > 0)
                .ToList();

            bool changed = false;
            foreach (var v in variantsWithImage)
            {
                bool exists = context.ProductImages.Any(pi => pi.ProductId == v.ProductId && pi.ColorId == v.ColorId && pi.DisplayOrder == 1);
                if (!exists)
                {
                    context.ProductImages.Add(new ProductImage
                    {
                        ProductId = v.ProductId,
                        ColorId = v.ColorId,
                        DisplayOrder = 1,
                        ImageUrl = v.ImageUrl!
                    });
                    changed = true;
                }
            }

            if (changed)
            {
                context.SaveChanges();
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

                var staff = new User
                {
                    Username = "nhanvien",
                    Email = "nhanvien@pcstore.vn",
                    FullName = "Nhân Viên Cửa Hàng",
                    PasswordHash = LaptopStore.Helpers.PasswordHelper.HashPassword("Staff@123"),
                    Role = "Staff",
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.AddRange(admin, staff, customer);
                context.SaveChanges();
                Console.WriteLine("[DbInitializer] Đã nạp thành công tài khoản mẫu: admin@pcstore.vn (Admin@123), nhanvien@pcstore.vn (Staff@123) và khachhang@gmail.com (User@123)");
            }
            else if (!context.Users.Any(u => u.Username == "nhanvien"))
            {
                var staff = new User
                {
                    Username = "nhanvien",
                    Email = "nhanvien@pcstore.vn",
                    FullName = "Nhân Viên Cửa Hàng",
                    PasswordHash = LaptopStore.Helpers.PasswordHelper.HashPassword("Staff@123"),
                    Role = "Staff",
                    CreatedAt = DateTime.UtcNow
                };
                context.Users.Add(staff);
                context.SaveChanges();
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

    private static void SeedColorsIfEmpty(AppDbContext context)
    {
        try
        {
            if (!context.Colors.Any())
            {
                var defaultColors = new List<Color>
                {
                    new() { Name = "Bạc", HexCode = "#E5E7EB", IsActive = true },
                    new() { Name = "Đen", HexCode = "#18181B", IsActive = true },
                    new() { Name = "Xám không gian", HexCode = "#4B5563", IsActive = true },
                    new() { Name = "Trắng", HexCode = "#F8FAFC", IsActive = true },
                    new() { Name = "Xanh navy", HexCode = "#1E3A8A", IsActive = true }
                };
                context.Colors.AddRange(defaultColors);
                context.SaveChanges();
                Console.WriteLine("[DbInitializer] Đã nạp bảng màu mặc định thành công.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DbInitializer] Lỗi SeedColorsIfEmpty: " + ex.Message);
        }
    }

    private static void SeedVariantsIfEmpty(AppDbContext context)
    {
        try
        {
            if (context.ProductVariants.Any()) return;
            if (!context.Products.Any() || !context.Colors.Any()) return;

            var colors = context.Colors.ToList();
            var silver = colors.FirstOrDefault(c => c.Name == "Bạc") ?? colors.First();
            var black = colors.FirstOrDefault(c => c.Name == "Đen") ?? colors.First();
            var spaceGray = colors.FirstOrDefault(c => c.Name == "Xám không gian") ?? colors.First();

            var products = context.Products
                .Include(p => p.Specification)
                .ToList();

            var variants = new List<ProductVariant>();

            foreach (var prod in products)
            {
                int ram = prod.Specification?.RamGB ?? 16;
                int storage = prod.Specification?.StorageGB ?? 512;

                bool isDellDemo = prod.ProductName.Contains("Dell", StringComparison.OrdinalIgnoreCase) || prod.ProductId == 1;

                if (isDellDemo)
                {
                    // Nghiệp vụ cốt lõi theo kịch bản:
                    // 1. Dell - Bạc - 16GB - 512GB: Tồn 1 (kế thừa giá máy: Price = null)
                    variants.Add(new ProductVariant
                    {
                        ProductId = prod.ProductId,
                        ColorId = silver.ColorId,
                        RamGB = 16,
                        StorageGB = 512,
                        SKU = $"DELL-{prod.ProductId}-SLV-16-512",
                        StockQuantity = 1,
                        Price = null, // Kế thừa giá gốc của sản phẩm
                        DiscountPrice = null,
                        ImageUrl = "/images/products/dell-inspiron-14-5430.png",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    // 2. Dell - Đen - 16GB - 512GB: Tồn 2 (kế thừa giá máy: Price = null)
                    variants.Add(new ProductVariant
                    {
                        ProductId = prod.ProductId,
                        ColorId = black.ColorId,
                        RamGB = 16,
                        StorageGB = 512,
                        SKU = $"DELL-{prod.ProductId}-BLK-16-512",
                        StockQuantity = 2,
                        Price = null, // Kế thừa giá gốc của sản phẩm
                        DiscountPrice = null,
                        ImageUrl = "/images/products/asus-tuf-gaming-f15-fx507zc4.png",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    // 3. Dell - Đen - 32GB - 1TB: Tồn 0 (cấu hình cao giá riêng, kế thừa ảnh đen từ SKU 16-512)
                    variants.Add(new ProductVariant
                    {
                        ProductId = prod.ProductId,
                        ColorId = black.ColorId,
                        RamGB = 32,
                        StorageGB = 1024,
                        SKU = $"DELL-{prod.ProductId}-BLK-32-1TB",
                        StockQuantity = 0,
                        Price = prod.Price + 4000000,
                        DiscountPrice = null,
                        ImageUrl = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Cập nhật tổng tồn kho của Dell = 1 + 2 + 0 = 3
                    prod.StockQuantity = 3;
                }
                else
                {
                    // Các laptop khác: Tạo 2 biến thể màu với thông số sẵn có, kế thừa giá sản phẩm
                    int halfStock = Math.Max(1, prod.StockQuantity / 2);
                    int remainingStock = Math.Max(0, prod.StockQuantity - halfStock);

                    variants.Add(new ProductVariant
                    {
                        ProductId = prod.ProductId,
                        ColorId = spaceGray.ColorId,
                        RamGB = ram,
                        StorageGB = storage,
                        SKU = $"SKU-{prod.ProductId}-SGRY-{ram}-{storage}",
                        StockQuantity = halfStock,
                        Price = null,
                        DiscountPrice = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    variants.Add(new ProductVariant
                    {
                        ProductId = prod.ProductId,
                        ColorId = silver.ColorId,
                        RamGB = ram,
                        StorageGB = storage,
                        SKU = $"SKU-{prod.ProductId}-SLV-{ram}-{storage}",
                        StockQuantity = remainingStock,
                        Price = null,
                        DiscountPrice = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            context.ProductVariants.AddRange(variants);
            context.SaveChanges();
            Console.WriteLine($"[DbInitializer] Đã nạp thành công {variants.Count} phiên bản (Variants) cho các laptop.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DbInitializer] Lỗi SeedVariantsIfEmpty: " + ex.Message);
        }
    }

    private static void SeedServiceCatalogIfEmpty(AppDbContext context)
    {
        try
        {
            if (context.ServiceCatalog.Any()) return;

            var services = new List<ServiceCatalogItem>
            {
                new() { ServiceCode = "DV-SC01", ServiceName = "Vệ sinh laptop & Tra keo tản nhiệt gốm MX4", ServiceType = "Sửa chữa", ApplicableConditions = "Máy nóng rực, quạt rú to, tự ngắt khi tải nặng", EstimatedCost = 150000, EstimatedTime = "30 - 45 phút", ServiceWarrantyMonths = 1, IsActive = true },
                new() { ServiceCode = "DV-SC02", ServiceName = "Thay màn hình Laptop Full HD IPS 144Hz", ServiceType = "Sửa chữa", ApplicableConditions = "Sọc màn hình, nhấp nháy chập chờn, nứt vỡ LCD", EstimatedCost = 1650000, EstimatedTime = "45 phút", ServiceWarrantyMonths = 6, IsActive = true },
                new() { ServiceCode = "DV-SC03", ServiceName = "Sửa nguồn bo mạch chủ (Mainboard) chạm IC", ServiceType = "Sửa chữa", ApplicableConditions = "Mất nguồn hoàn toàn, cắm sạc đèn không sáng", EstimatedCost = 650000, EstimatedTime = "1 - 2 ngày", ServiceWarrantyMonths = 3, IsActive = true },
                new() { ServiceCode = "DV-SC04", ServiceName = "Thay cụm bàn phím laptop chuẩn hãng", ServiceType = "Sửa chữa", ApplicableConditions = "Liệt phím, kẹt phím, tự động nhảy ký tự liên tục", EstimatedCost = 450000, EstimatedTime = "40 phút", ServiceWarrantyMonths = 12, IsActive = true },
                new() { ServiceCode = "DV-SC05", ServiceName = "Thay pin laptop zin dung lượng cao", ServiceType = "Sửa chữa", ApplicableConditions = "Pin chai phồng, pin ảo dùng 15 phút sập nguồn", EstimatedCost = 780000, EstimatedTime = "30 phút", ServiceWarrantyMonths = 6, IsActive = true },
                new() { ServiceCode = "DV-SC06", ServiceName = "Hàn phục hồi chân ốc & bản lề gãy vỡ", ServiceType = "Sửa chữa", ApplicableConditions = "Vỏ bung cọt kẹt khi gập mở, hở mép viền", EstimatedCost = 350000, EstimatedTime = "2 - 3 giờ", ServiceWarrantyMonths = 3, IsActive = true },
                new() { ServiceCode = "DV-SC07", ServiceName = "Nâng cấp ổ cứng SSD NVMe PCIe 512GB", ServiceType = "Sửa chữa", ApplicableConditions = "Máy khởi động rất chậm, ổ HDD cũ bị Bad Sector", EstimatedCost = 890000, EstimatedTime = "30 phút", ServiceWarrantyMonths = 36, IsActive = true },
                new() { ServiceCode = "DV-SC08", ServiceName = "Đóng lại / Thay thế Chipset VGA đồ họa rời", ServiceType = "Sửa chữa", ApplicableConditions = "Rác hình, màn hình xanh BSOD khi bật game", EstimatedCost = 1250000, EstimatedTime = "2 - 3 ngày", ServiceWarrantyMonths = 3, IsActive = true },

                new() { ServiceCode = "DV-BH01", ServiceName = "Bảo hành đổi mới Pin linh kiện chính hãng", ServiceType = "Bảo hành", ApplicableConditions = "Pin chai vượt mức 30% trong thời hạn bảo hành hãng", EstimatedCost = 0, EstimatedTime = "1 - 2 ngày", ServiceWarrantyMonths = 12, IsActive = true },
                new() { ServiceCode = "DV-BH02", ServiceName = "Bảo hành lỗi điểm chết & sọc màn hình LCD", ServiceType = "Bảo hành", ApplicableConditions = "Xuất hiện trên 3 điểm chết hoặc sọc chỉ màn hình", EstimatedCost = 0, EstimatedTime = "3 - 5 ngày", ServiceWarrantyMonths = 12, IsActive = true },
                new() { ServiceCode = "DV-BH03", ServiceName = "Bảo hành bàn phím & Touchpad theo tiêu chuẩn hãng", ServiceType = "Bảo hành", ApplicableConditions = "Liệt cụm phím, chập chuột cảm ứng do lỗi linh kiện", EstimatedCost = 0, EstimatedTime = "2 - 4 ngày", ServiceWarrantyMonths = 12, IsActive = true },
                new() { ServiceCode = "DV-BH04", ServiceName = "Bảo hành loa trong & cụm Micro đàm thoại", ServiceType = "Bảo hành", ApplicableConditions = "Rè loa, mất tín hiệu mic thu âm không do ẩm nước", EstimatedCost = 0, EstimatedTime = "1 - 2 ngày", ServiceWarrantyMonths = 12, IsActive = true },
                new() { ServiceCode = "DV-BH05", ServiceName = "Bảo hành Bo mạch chủ (Mainboard)", ServiceType = "Bảo hành", ApplicableConditions = "Lỗi nguồn chip, không nhận SSD/RAM theo tem bảo hành", EstimatedCost = 0, EstimatedTime = "5 - 7 ngày", ServiceWarrantyMonths = 12, IsActive = true },
                new() { ServiceCode = "DV-BH06", ServiceName = "Bảo hành sạc Adapter theo máy", ServiceType = "Bảo hành", ApplicableConditions = "Không sạc vào điện, tiếp xúc chập chờn nguyên tem", EstimatedCost = 0, EstimatedTime = "Đổi mới ngay", ServiceWarrantyMonths = 12, IsActive = true }
            };

            context.ServiceCatalog.AddRange(services);
            context.SaveChanges();
            Console.WriteLine($"[DbInitializer] Đã nạp thành công {services.Count} dịch vụ Sửa chữa & Bảo hành.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DbInitializer] Cảnh báo SeedServiceCatalogIfEmpty: " + ex.Message);
        }
    }

    private static void SeedSampleSoldDevicesIfEmpty(AppDbContext context)
    {
        try
        {
            if (context.SoldDevices.Any()) return;

            // Tìm các đơn hàng để gắn số Serial mẫu cho máy đã giao/bán
            var completedOrders = context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.OrderStatus == "Hoàn thành" || o.OrderStatus == "Đã xác nhận" || o.OrderStatus == "Đang xử lý")
                .ToList();

            if (!completedOrders.Any())
            {
                completedOrders = context.Orders.Include(o => o.OrderItems).ToList();
            }

            if (!completedOrders.Any()) return;

            var devices = new List<SoldDevice>();
            int serialCounter = 1001;

            foreach (var order in completedOrders)
            {
                foreach (var item in order.OrderItems)
                {
                    int qty = Math.Max(1, item.Quantity);
                    for (int i = 0; i < qty; i++)
                    {
                        var prefix = item.ProductName.Contains("Dell", StringComparison.OrdinalIgnoreCase) ? "DELL" :
                                     item.ProductName.Contains("Asus", StringComparison.OrdinalIgnoreCase) ? "ASUS" :
                                     item.ProductName.Contains("MacBook", StringComparison.OrdinalIgnoreCase) ? "APPL" : "LPT";
                        
                        devices.Add(new SoldDevice
                        {
                            OrderItemId = item.OrderItemId,
                            SerialNumber = $"SN-{prefix}-{serialCounter++}",
                            DeliveredAt = order.CreatedAt,
                            WarrantyStartDate = order.CreatedAt,
                            WarrantyEndDate = order.CreatedAt.AddMonths(12),
                            WarrantyMonths = 12,
                            Notes = $"Xuất xưởng kèm đơn #{order.OrderCode} - {item.ProductName}"
                        });
                    }
                }
            }

            if (devices.Any())
            {
                context.SoldDevices.AddRange(devices);
                context.SaveChanges();
                Console.WriteLine($"[DbInitializer] Đã gắn thành công {devices.Count} số Serial cho các laptop đã giao.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DbInitializer] Cảnh báo SeedSampleSoldDevicesIfEmpty: " + ex.Message);
        }
    }

    public static void SeedDiscountsIfEmpty(AppDbContext context)
    {
        try
        {
            if (context.Discounts.Any()) return;

            var discounts = new List<Discount>
            {
                new Discount
                {
                    Code = "LAPTOP2026",
                    Title = "Ưu đãi Laptop Học tập - Văn phòng",
                    Description = "Giảm 10% (tối đa 1.500.000đ) cho đơn hàng laptop Văn phòng & Học tập từ 10 triệu.",
                    DiscountType = "Percentage",
                    DiscountValue = 10,
                    MaxDiscountAmount = 1500000,
                    MinOrderAmount = 10000000,
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    UsageLimit = 500,
                    UsedCount = 45,
                    ApplicableScope = "OfficeOnly",
                    IsActive = true
                },
                new Discount
                {
                    Code = "GAMING500K",
                    Title = "Đại tiệc Laptop Gaming",
                    Description = "Giảm ngay 500.000đ khi mua Laptop Gaming ASUS, Acer, MSI, Lenovo từ 18 triệu.",
                    DiscountType = "FixedAmount",
                    DiscountValue = 500000,
                    MaxDiscountAmount = 500000,
                    MinOrderAmount = 18000000,
                    StartDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    UsageLimit = 200,
                    UsedCount = 89,
                    ApplicableScope = "GamingOnly",
                    IsActive = true
                },
                new Discount
                {
                    Code = "APPLE",
                    Title = "Trợ giá mua sắm Apple MacBook",
                    Description = "Giảm ngay 2.000.000đ khi mua Apple MacBook M2/M3 chính hãng từ 25 triệu.",
                    DiscountType = "FixedAmount",
                    DiscountValue = 2000000,
                    MaxDiscountAmount = 2000000,
                    MinOrderAmount = 25000000,
                    StartDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    UsageLimit = 100,
                    UsedCount = 30,
                    ApplicableScope = "AppleOnly",
                    IsActive = true
                },
                new Discount
                {
                    Code = "WELCOME",
                    Title = "Chào mừng thành viên mới",
                    Description = "Giảm 3% (tối đa 200.000đ) cho đơn hàng đầu tiên của khách hàng mới từ 5 triệu.",
                    DiscountType = "Percentage",
                    DiscountValue = 3,
                    MaxDiscountAmount = 200000,
                    MinOrderAmount = 5000000,
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    UsageLimit = null,
                    UsedCount = 120,
                    ApplicableScope = "FirstOrderOnly",
                    IsActive = true
                },
                new Discount
                {
                    Code = "TRIAN99",
                    Title = "Tri ân khách hàng thân thiết",
                    Description = "Giảm 200.000đ cho đơn hàng từ 10.000.000đ.",
                    DiscountType = "FixedAmount",
                    DiscountValue = 200000,
                    MaxDiscountAmount = 200000,
                    MinOrderAmount = 10000000,
                    StartDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc), // Hết hạn ngày 31/10/2026
                    UsageLimit = 200,
                    UsedCount = 89,
                    ApplicableScope = "All",
                    IsActive = true
                },
                new Discount
                {
                    Code = "GAMINGX",
                    Title = "Flash Sale Laptop Gaming Giới Hạn",
                    Description = "Giảm ngay 1.000.000đ cho khách hàng mua Laptop Gaming (giới hạn 10 suất).",
                    DiscountType = "FixedAmount",
                    DiscountValue = 1000000,
                    MaxDiscountAmount = 1000000,
                    MinOrderAmount = 18000000,
                    StartDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 10, 15, 23, 59, 59, DateTimeKind.Utc), // Đã hết hạn & hết lượt
                    UsageLimit = 10,
                    UsedCount = 10,
                    ApplicableScope = "GamingOnly",
                    IsActive = true
                }
            };

            context.Discounts.AddRange(discounts);
            context.SaveChanges();
            Console.WriteLine($"[DbInitializer] Đã nạp thành công {discounts.Count} mã giảm giá vào CSDL.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DbInitializer] Cảnh báo SeedDiscountsIfEmpty: " + ex.Message);
        }
    }

    private static void SyncUserPhoneAndLinkOrders(AppDbContext context)
    {
        try
        {
            var users = context.Users.ToList();
            bool hasChanges = false;
            foreach (var user in users)
            {
                // Tìm số điện thoại từ đơn hàng nếu user chưa có
                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    var orderPhone = context.Orders
                        .Where(o => (o.UserId == user.UserId || (!string.IsNullOrEmpty(user.Email) && o.Email == user.Email) || (!string.IsNullOrEmpty(user.FullName) && o.CustomerName == user.FullName)) && !string.IsNullOrEmpty(o.PhoneNumber))
                        .OrderByDescending(o => o.OrderId)
                        .Select(o => o.PhoneNumber)
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(orderPhone))
                    {
                        user.PhoneNumber = orderPhone;
                        hasChanges = true;
                    }
                }

                // Tự động liên kết các đơn hàng cũ chưa có UserId
                if (!string.IsNullOrEmpty(user.PhoneNumber) || !string.IsNullOrEmpty(user.Email) || !string.IsNullOrEmpty(user.FullName))
                {
                    var unlinkedOrders = context.Orders
                        .Where(o => o.UserId == null && (
                            (!string.IsNullOrEmpty(user.PhoneNumber) && o.PhoneNumber == user.PhoneNumber) ||
                            (!string.IsNullOrEmpty(user.Email) && o.Email == user.Email) ||
                            (!string.IsNullOrEmpty(user.FullName) && o.CustomerName == user.FullName)
                        ))
                        .ToList();

                    if (unlinkedOrders.Any())
                    {
                        foreach (var uo in unlinkedOrders)
                        {
                            uo.UserId = user.UserId;
                        }
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DbInitializer] Lỗi SyncUserPhoneAndLinkOrders: " + ex.Message);
        }
    }
}
