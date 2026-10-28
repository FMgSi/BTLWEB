-- =========================================================================
-- KHỞI TẠO CƠ SỞ DỮ LIỆU MICROSOFT SQL SERVER CHO LAPTOPSTORE
-- Hỗ trợ đầy đủ Unicode tiếng Việt (NVARCHAR), chuẩn hóa 3NF, ràng buộc khóa ngoại
-- =========================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'LaptopStoreDb')
BEGIN
    CREATE DATABASE LaptopStoreDb COLLATE Vietnamese_CI_AS;
END
GO

USE LaptopStoreDb;
GO

-- 1. Bảng Thương hiệu (Brands)
IF OBJECT_ID('dbo.Brands', 'U') IS NULL
CREATE TABLE Brands (
    BrandId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    BrandName NVARCHAR(255) NOT NULL,
    LogoUrl NVARCHAR(MAX) NULL,
    Description NVARCHAR(MAX) NULL
);
GO

-- 2. Bảng Danh mục (Categories)
IF OBJECT_ID('dbo.Categories', 'U') IS NULL
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CategoryName NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL
);
GO

-- 3. Bảng Sản phẩm (Products)
IF OBJECT_ID('dbo.Products', 'U') IS NULL
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductName NVARCHAR(200) NOT NULL,
    BrandId INT NOT NULL,
    CategoryId INT NOT NULL,
    Price DECIMAL(18, 2) NOT NULL,
    DiscountPrice DECIMAL(18, 2) NULL,
    StockQuantity INT NOT NULL,
    ThumbnailUrl NVARCHAR(MAX) NULL,
    Description NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Products_Brands_BrandId FOREIGN KEY (BrandId) REFERENCES Brands (BrandId) ON DELETE CASCADE,
    CONSTRAINT FK_Products_Categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES Categories (CategoryId) ON DELETE CASCADE
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_BrandId' AND object_id = OBJECT_ID('Products'))
    CREATE NONCLUSTERED INDEX IX_Products_BrandId ON Products (BrandId);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CategoryId' AND object_id = OBJECT_ID('Products'))
    CREATE NONCLUSTERED INDEX IX_Products_CategoryId ON Products (CategoryId);
GO

-- 4. Bảng Cấu hình chi tiết (ProductSpecifications - Quan hệ 1-1 với Products)
IF OBJECT_ID('dbo.ProductSpecifications', 'U') IS NULL
CREATE TABLE ProductSpecifications (
    ProductId INT NOT NULL PRIMARY KEY,
    CPU NVARCHAR(MAX) NULL,
    RamGB INT NOT NULL,
    StorageGB INT NOT NULL,
    StorageType NVARCHAR(MAX) NULL,
    GPU NVARCHAR(MAX) NULL,
    ScreenSizeInch DECIMAL(18, 2) NOT NULL,
    RefreshRateHz INT NOT NULL,
    WeightKg DECIMAL(18, 2) NOT NULL,
    BatteryWh INT NOT NULL,
    OS NVARCHAR(MAX) NULL,
    CONSTRAINT FK_ProductSpecifications_Products_ProductId FOREIGN KEY (ProductId) REFERENCES Products (ProductId) ON DELETE CASCADE
);
GO

-- 5. Bảng Ảnh sản phẩm bổ sung (ProductImages)
IF OBJECT_ID('dbo.ProductImages', 'U') IS NULL
CREATE TABLE ProductImages (
    ImageId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL,
    ImageUrl NVARCHAR(MAX) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_ProductImages_Products_ProductId FOREIGN KEY (ProductId) REFERENCES Products (ProductId) ON DELETE CASCADE
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductImages_ProductId' AND object_id = OBJECT_ID('ProductImages'))
    CREATE NONCLUSTERED INDEX IX_ProductImages_ProductId ON ProductImages (ProductId);
GO

-- 6. Bảng Tài khoản người dùng (Users)
IF OBJECT_ID('dbo.Users', 'U') IS NULL
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'Customer',
    GoogleId NVARCHAR(100) NULL,
    AvatarUrl NVARCHAR(255) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT IX_Users_Username UNIQUE (Username),
    CONSTRAINT IX_Users_Email UNIQUE (Email)
);
GO

-- 7. Bảng Đơn đặt hàng (Orders)
IF OBJECT_ID('dbo.Orders', 'U') IS NULL
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderCode NVARCHAR(50) NOT NULL,
    UserId INT NULL,
    CustomerName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    Email NVARCHAR(100) NULL,
    ShippingAddress NVARCHAR(255) NOT NULL,
    Notes NVARCHAR(500) NULL,
    SubTotal DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'COD',
    PaymentStatus NVARCHAR(50) NOT NULL DEFAULT N'Chờ thanh toán',
    OrderStatus NVARCHAR(50) NOT NULL DEFAULT N'Đang xử lý',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,
    CONSTRAINT IX_Orders_OrderCode UNIQUE (OrderCode),
    CONSTRAINT FK_Orders_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (UserId) ON DELETE SET NULL
);
GO

-- 8. Bảng Chi tiết mặt hàng trong đơn (OrderItems)
IF OBJECT_ID('dbo.OrderItems', 'U') IS NULL
CREATE TABLE OrderItems (
    OrderItemId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    SelectedSpecs NVARCHAR(255) NULL,
    ThumbnailUrl NVARCHAR(500) NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    TotalPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderItems_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES Orders (OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products_ProductId FOREIGN KEY (ProductId) REFERENCES Products (ProductId) ON DELETE NO ACTION
);
GO

-- 9. Bảng Giao dịch thanh toán (Payments)
IF OBJECT_ID('dbo.Payments', 'U') IS NULL
CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderId INT NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'COD',
    Amount DECIMAL(18,2) NOT NULL,
    TransactionReference NVARCHAR(100) NULL,
    PaymentStatus NVARCHAR(50) NOT NULL DEFAULT N'Chờ thanh toán',
    PaymentDate DATETIME NOT NULL DEFAULT GETDATE(),
    Note NVARCHAR(255) NULL,
    CONSTRAINT FK_Payments_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES Orders (OrderId) ON DELETE CASCADE
);
GO

-- 10. Bảng Nhật ký trạng thái đơn (OrderStatusHistories)
IF OBJECT_ID('dbo.OrderStatusHistories', 'U') IS NULL
CREATE TABLE OrderStatusHistories (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderId INT NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    ChangedBy NVARCHAR(100) NULL,
    ChangedAt DATETIME NOT NULL DEFAULT GETDATE(),
    Note NVARCHAR(500) NULL,
    CONSTRAINT FK_OrderStatusHistories_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES Orders (OrderId) ON DELETE CASCADE
);
GO

-- =============================================
-- Brands (Bật IDENTITY_INSERT để nạp ID định sẵn)
-- =============================================
SET IDENTITY_INSERT Brands ON;
GO

-- 1. Brands
INSERT INTO Brands (BrandId, BrandName, LogoUrl, Description) VALUES
(1, N'ASUS', N'/images/brands/asus.png', N'Thương hiệu hàng đầu với dòng ROG, TUF Gaming, Zenbook, Vivobook.'),
(2, N'Dell', N'/images/brands/dell.png', N'Nổi tiếng bền bỉ với dòng XPS cao cấp, Alienware gaming và Inspiron/Vostro.'),
(3, N'Lenovo', N'/images/brands/lenovo.png', N'Dòng Legion gaming đỉnh cao, ThinkPad doanh nhân và IdeaPad/Yoga.'),
(4, N'Apple', N'/images/brands/apple.png', N'Dòng MacBook Air, MacBook Pro cao cấp chạy chip Apple Silicon độc quyền.'),
(5, N'Acer', N'/images/brands/acer.png', N'Thế mạnh cấu hình cao giá tốt với Nitro 5/V, Predator Helios và Swift.'),
(6, N'HP', N'/images/brands/hp.png', N'Đa dạng phân khúc với Victus gaming, OMEN, Pavilion, Envy và Spectre cao cấp.'),
(7, N'MSI', N'/images/brands/msi.png', N'Chuyên laptop gaming Katana, Cyborg, Raider, Stealth và dòng Modern văn phòng.'),
(8, N'LG', N'/images/brands/lg.png', N'Dòng LG gram siêu nhẹ, pin khủng, độ bền cao cho doanh nhân và văn phòng.'),
(9, N'Gigabyte', N'/images/brands/gigabyte.png', N'Laptop gaming cấu hình khỏe giá cạnh tranh G5/G6 và Aorus chuyên nghiệp.'),
(10, N'Microsoft', N'/images/brands/microsoft.png', N'Dòng Surface sang trọng, màn hình cảm ứng sắc nét tỉ lệ 3:2 độc đáo.');

GO

SET IDENTITY_INSERT Brands OFF;
GO

-- =============================================
-- Categories (Bật IDENTITY_INSERT để nạp ID định sẵn)
-- =============================================
SET IDENTITY_INSERT Categories ON;
GO

-- 2. Categories
INSERT INTO Categories (CategoryId, CategoryName, Description) VALUES
(1, N'Laptop Gaming', N'Trang bị CPU hiệu năng cao (dòng H/HX), GPU rời NVIDIA GeForce RTX, tản nhiệt buồng hơi, màn hình tần số quét từ 144Hz trở lên.'),
(2, N'Laptop Học tập - Văn phòng', N'Thiết kế thực dụng, thời lượng pin tốt, bàn phím gõ êm, tối ưu tác vụ tin học văn phòng, lướt web, học online.'),
(3, N'Laptop Đồ họa - Kỹ thuật', N'Màn hình chuẩn màu sRGB/DCI-P3, CPU/GPU mạnh mẽ tối ưu cho AutoCAD, SolidWorks, Premiere, lập trình và render.'),
(4, N'Laptop Mỏng nhẹ - Doanh nhân', N'Trọng lượng dưới 1.4kg, vỏ kim loại/magie cao cấp, độ bảo mật cao (vân tay, nhận diện khuôn mặt), thiết kế sang trọng.'),
(5, N'Workstation Di Động', N'Trạm làm việc di động với CPU cực mạnh, RAM mở rộng lớn, GPU chuyên dụng Studio/Quadro, độ bền chuẩn quân đội.');

GO

SET IDENTITY_INSERT Categories OFF;
GO

-- =============================================
-- Products (Bật IDENTITY_INSERT để nạp ID định sẵn)
-- =============================================
SET IDENTITY_INSERT Products ON;
GO

-- 3. Products
INSERT INTO Products (ProductId, ProductName, BrandId, CategoryId, Price, DiscountPrice, StockQuantity, ThumbnailUrl, Description, IsActive) VALUES
(1, N'ASUS TUF Gaming F15 FX507ZC4', 1, 1, 21990000.00, 18490000.00, 45, N'/images/products/asus-tuf-gaming-f15-fx507zc4.png', N'Laptop gaming quốc dân phân khúc 18 triệu, tản nhiệt mát mẻ.', 1),
(2, N'ASUS TUF Gaming A15 FA506NC', 1, 1, 19990000.00, 16990000.00, 12, N'/images/products/asus-tuf-gaming-a15-fa506nc.png', N'Cấu hình Ryzen 5 thế hệ 7000 kết hợp RTX 3050 cho gaming mượt mà.', 1),
(3, N'ASUS TUF Gaming A15 FA507NV', 1, 1, 29990000.00, 26490000.00, 6, N'/images/products/asus-tuf-gaming-a15-fa507nv.png', N'Pin khủng 90Wh kết hợp card RTX 4060 8GB cân mọi game AAA.', 1),
(4, N'ASUS TUF Gaming F16 FX607JU', 1, 1, 33990000.00, 29990000.00, 22, N'/images/products/asus-tuf-gaming-f16-fx607ju.png', N'Màn hình 16:10 165Hz chuẩn gaming thế hệ mới, CPU 14 nhân.', 1),
(5, N'ASUS ROG Strix G16 G614JU', 1, 1, 39990000.00, 36490000.00, 20, N'/images/products/asus-rog-strix-g16-g614ju.png', N'Dải LED Aura Sync nổi bật, công nghệ tản nhiệt 3 quạt Tri-Fan.', 1),
(6, N'ASUS ROG Strix G16 G614JVR', 1, 1, 56990000.00, 52990000.00, 19, N'/images/products/asus-rog-strix-g16-g614jvr.png', N'Quái vật gaming i9 thế hệ 14 kết hợp RTX 4080 cực mạnh.', 1),
(7, N'ASUS ROG Strix SCAR 16 G634JZR', 1, 1, 89990000.00, 84990000.00, 13, N'/images/products/asus-rog-strix-scar-16-g634jzr.png', N'Màn hình Mini-LED ROG Nebula HDR 1100 nits siêu đỉnh.', 1),
(8, N'ASUS ROG Strix SCAR 18 G834JYR', 1, 1, 125000000.00, 119900000.00, 11, N'/images/products/asus-rog-strix-scar-18-g834jyr.png', N'Flagship đỉnh nóc kịch trần màn hình 18 inch RTX 4090.', 1),
(9, N'ASUS ROG Zephyrus G14 GA403UI', 1, 1, 54990000.00, 49990000.00, 48, N'/images/products/asus-rog-zephyrus-g14-ga403ui.png', N'Laptop gaming 14 inch mỏng nhẹ vỏ nhôm CNC màn hình OLED.', 1),
(10, N'ASUS ROG Zephyrus G16 GU605MI', 1, 1, 59990000.00, 54990000.00, 39, N'/images/products/asus-rog-zephyrus-g16-gu605mi.png', N'Chip AI Core Ultra 7, màn hình OLED 2.5K 240Hz siêu mỏng.', 1),
(11, N'ASUS ROG Flow Z13 GZ301VU', 1, 1, 45990000.00, 41990000.00, 10, N'/images/products/asus-rog-flow-z13-gz301vu.png', N'Tablet gaming mạnh nhất thế giới có bàn phím tháo rời.', 1),
(12, N'ASUS ROG Flow X16 GV601VV', 1, 1, 52990000.00, 47990000.00, 42, N'/images/products/asus-rog-flow-x16-gv601vv.png', N'Laptop gaming xoay gập 360 độ màn hình cảm ứng Touch.', 1),
(13, N'ASUS Vivobook Go 14 E1404FA', 1, 2, 12990000.00, 10990000.00, 32, N'/images/products/asus-vivobook-go-14-e1404fa.png', N'Mẫu máy văn phòng quốc dân giá rẻ 16GB RAM.', 1),
(14, N'ASUS Vivobook Go 15 E1504FA', 1, 2, 13490000.00, 11490000.00, 7, N'/images/products/asus-vivobook-go-15-e1504fa.png', N'Màn hình lớn 15.6 inch có bàn phím số phụ tiện nhập liệu.', 1),
(15, N'ASUS Vivobook 14 X1404VA', 1, 2, 15990000.00, 13990000.00, 6, N'/images/products/asus-vivobook-14-x1404va.png', N'Thiết kế bản lề 180 độ, cảm biến vân tay 1 chạm.', 1),
(16, N'ASUS Vivobook 15 X1504VA', 1, 2, 16990000.00, 14790000.00, 10, N'/images/products/asus-vivobook-15-x1504va.png', N'Máy làm việc văn phòng đa nhiệm mượt mà, viền mỏng NanoEdge.', 1),
(17, N'ASUS Vivobook 16 X1605VA', 1, 2, 18990000.00, 16490000.00, 18, N'/images/products/asus-vivobook-16-x1605va.png', N'Màn hình 16 inch không gian làm việc rộng rãi kèm chip i7 dòng H.', 1),
(18, N'ASUS Vivobook S 14 OLED S5406MA', 1, 2, 27990000.00, 25490000.00, 19, N'/images/products/asus-vivobook-s-14-oled-s5406ma.png', N'Màn hình OLED 3K 120Hz rực rỡ, pin trâu 75Wh.', 1),
(19, N'ASUS Vivobook S 16 OLED S5606MA', 1, 2, 31990000.00, 28990000.00, 37, N'/images/products/asus-vivobook-s-16-oled-s5606ma.png', N'Laptop AI thế hệ mới với phím Copilot chuyên dụng.', 1),
(20, N'ASUS ExpertBook B1 B1402CVA', 1, 2, 16500000.00, 14900000.00, 43, N'/images/products/asus-expertbook-b1-b1402cva.png', N'Dòng doanh nghiệp bền bỉ, tích hợp chip bảo mật TPM 2.0.', 1),
(21, N'ASUS ExpertBook B9 OLED B9403CVAR', 1, 2, 49990000.00, 46990000.00, 6, N'/images/products/asus-expertbook-b9-oled-b9403cvar.png', N'Laptop doanh nhân siêu nhẹ dưới 1kg làm bằng hợp kim Magie-Liti.', 1),
(22, N'ASUS Vivobook Pro 15 OLED N6506MV', 1, 3, 38990000.00, 35490000.00, 40, N'/images/products/asus-vivobook-pro-15-oled-n6506mv.png', N'Laptop đồ họa mỏng nhẹ có núm xoay ảo ASUS DialPad.', 1),
(23, N'ASUS Vivobook Pro 16X OLED K6604JI', 1, 3, 52990000.00, 48990000.00, 17, N'/images/products/asus-vivobook-pro-16x-oled-k6604ji.png', N'Cấu hình cực mạnh chuyên dựng phim 4K và kiến trúc.', 1),
(24, N'ASUS ProArt Studiobook 16 OLED H7604JI', 1, 3, 72990000.00, 68990000.00, 50, N'/images/products/asus-proart-studiobook-16-oled-h7604ji.png', N'Máy trạm đồ họa chuyên nghiệp màn hình OLED cảm ứng có bút stylus.', 1),
(25, N'ASUS ProArt P16 H7606WI', 1, 3, 79990000.00, 75990000.00, 46, N'/images/products/asus-proart-p16-h7606wi.png', N'Chip AI Ryzen 9 thế hệ mới kết hợp 64GB RAM cho render nặng.', 1),
(26, N'ASUS ProArt PX13 HN7306WV', 1, 3, 62990000.00, 58990000.00, 49, N'/images/products/asus-proart-px13-hn7306wv.png', N'Laptop đồ họa xoay gập 360 độ 13 inch mạnh nhất thế giới.', 1),
(27, N'ASUS Zenbook Pro 14 OLED UX6404VI', 1, 3, 56990000.00, 51990000.00, 39, N'/images/products/asus-zenbook-pro-14-oled-ux6404vi.png', N'Nhỏ gọn nhưng mang hiệu năng của máy trạm đồ họa.', 1),
(28, N'ASUS Zenbook Pro Duo 15 UX582ZM', 1, 3, 65000000.00, 59900000.00, 31, N'/images/products/asus-zenbook-pro-duo-15-ux582zm.png', N'Thiết kế 2 màn hình 4K OLED độc nhất vô nhị cho creator.', 1),
(29, N'ASUS Zenbook 14 OLED UX3405MA', 1, 4, 28990000.00, 26990000.00, 19, N'/images/products/asus-zenbook-14-oled-ux3405ma.png', N'Tuyệt tác nhôm nguyên khối, pin dùng liên tục 15 tiếng.', 1),
(30, N'ASUS Zenbook 14 OLED UX3405MA-i7', 1, 4, 34990000.00, 31990000.00, 33, N'/images/products/asus-zenbook-14-oled-ux3405ma-i7.png', N'Phiên bản cao cấp 32GB RAM màn hình 3K Lumina OLED 120Hz.', 1),
(31, N'ASUS Zenbook S 13 OLED UX5304MA', 1, 4, 39990000.00, 36990000.00, 42, N'/images/products/asus-zenbook-s-13-oled-ux5304ma.png', N'Laptop OLED mỏng nhẹ nhất thế giới, dày chỉ 1cm nặng đúng 1kg.', 1),
(32, N'ASUS Zenbook S 16 UM5606WA', 1, 4, 44990000.00, 41990000.00, 22, N'/images/products/asus-zenbook-s-16-um5606wa.png', N'Chất liệu gốm nhôm Ceraluminum độc quyền siêu sang trọng.', 1),
(33, N'ASUS Zenbook DUO UX8406MA', 1, 4, 49990000.00, 46990000.00, 5, N'/images/products/asus-zenbook-duo-ux8406ma.png', N'Laptop 2 màn hình cảm ứng 14 inch OLED tháo rời bàn phím.', 1),
(34, N'ASUS Zenbook 14 Flip OLED UP3404VA', 1, 4, 33990000.00, 29990000.00, 15, N'/images/products/asus-zenbook-14-flip-oled-up3404va.png', N'Xoay gập 360 độ linh hoạt kèm bút cảm ứng ASUS Pen 2.0.', 1),
(35, N'ASUS ExpertBook B5 B5404CMA', 1, 4, 29990000.00, 27500000.00, 49, N'/images/products/asus-expertbook-b5-b5404cma.png', N'Doanh nhân văn phòng cao cấp chuẩn Intel Evo.', 1),
(36, N'Dell Gaming G15 5530 i5', 2, 1, 24990000.00, 21990000.00, 32, N'/images/products/dell-gaming-g15-5530-i5.png', N'Laptop gaming Dell hầm hố, khung máy lấy cảm hứng từ Alienware.', 1),
(37, N'Dell Gaming G15 5530 i7', 2, 1, 33990000.00, 29990000.00, 26, N'/images/products/dell-gaming-g15-5530-i7.png', N'Trang bị màn hình 165Hz sRGB 100% cùng pin 86Wh.', 1),
(38, N'Dell Gaming G16 7630', 2, 1, 44990000.00, 39990000.00, 22, N'/images/products/dell-gaming-g16-7630.png', N'Màn hình 16 inch QHD+ 240Hz, bàn phím cơ Cherry MX.', 1),
(39, N'Dell Alienware m16 R2', 2, 1, 59990000.00, 54990000.00, 14, N'/images/products/dell-alienware-m16-r2.png', N'Huyền thoại người ngoài hành tinh Alienware thiết kế tàng hình Stealth Mode.', 1),
(40, N'Dell Alienware x16 R2', 2, 1, 84990000.00, 79990000.00, 18, N'/images/products/dell-alienware-x16-r2.png', N'Siêu phẩm gaming cao cấp nhất của Dell, vỏ nhôm Lunar Silver.', 1),
(41, N'Dell Alienware m18 R2', 2, 1, 105000000.00, 99990000.00, 26, N'/images/products/dell-alienware-m18-r2.png', N'Trùm cuối Alienware 18 inch thay thế hoàn toàn máy tính để bàn.', 1),
(42, N'Dell Inspiron 15 3520 i3', 2, 2, 12490000.00, 10490000.00, 11, N'/images/products/dell-inspiron-15-3520-i3.png', N'Laptop phổ thông cho học sinh, sinh viên, màn hình 120Hz mượt mà.', 1),
(43, N'Dell Inspiron 15 3520 i5', 2, 2, 15990000.00, 13790000.00, 10, N'/images/products/dell-inspiron-15-3520-i5.png', N'Bản 16GB RAM chạy đa nhiệm mượt mà, độ bền chuẩn Dell.', 1),
(44, N'Dell Inspiron 14 5430', 2, 2, 20990000.00, 18490000.00, 29, N'/images/products/dell-inspiron-14-5430.png', N'Vỏ nhôm bạc thanh lịch, tỷ lệ màn hình 16:10 hiển thị nhiều dòng.', 1),
(45, N'Dell Inspiron 16 5630', 2, 2, 24990000.00, 21990000.00, 11, N'/images/products/dell-inspiron-16-5630.png', N'Màn hình 16 inch FHD+ kèm loa Waves MaxxAudio Pro to rõ.', 1),
(46, N'Dell Inspiron 14 7440 2-in-1', 2, 2, 23990000.00, 20990000.00, 27, N'/images/products/dell-inspiron-14-7440-2-in-1.png', N'Xoay gập cảm ứng linh hoạt phục vụ ghi chú và thuyết trình.', 1),
(47, N'Dell Vostro 14 3430', 2, 2, 14990000.00, 12990000.00, 27, N'/images/products/dell-vostro-14-3430.png', N'Dòng văn phòng doanh nghiệp nhỏ, bàn phím chống tràn nước.', 1),
(48, N'Dell Vostro 15 3530', 2, 2, 16490000.00, 14490000.00, 43, N'/images/products/dell-vostro-15-3530.png', N'Màn hình chống chói 120Hz làm việc thời gian dài không mỏi mắt.', 1),
(49, N'Dell Latitude 3440', 2, 2, 17500000.00, 15900000.00, 21, N'/images/products/dell-latitude-3440.png', N'Dòng máy tiêu chuẩn quân sự cho khối văn phòng ngân hàng.', 1),
(50, N'Dell Latitude 5440', 2, 2, 25900000.00, 23500000.00, 7, N'/images/products/dell-latitude-5440.png', N'Bảo mật vân tay và camera hồng ngoại IR nhận diện khuôn mặt.', 1),
(51, N'Dell Inspiron 16 Plus 7630', 2, 3, 38990000.00, 34990000.00, 34, N'/images/products/dell-inspiron-16-plus-7630.png', N'Màn hình 2.5K 120Hz sRGB 100% chuyên làm đồ họa và video.', 1),
(52, N'Dell XPS 15 9530 i7', 2, 3, 56990000.00, 51990000.00, 39, N'/images/products/dell-xps-15-9530-i7.png', N'Biểu tượng đồ họa mỏng nhẹ với viền màn hình vô cực InfinityEdge.', 1),
(53, N'Dell XPS 15 9530 OLED', 2, 3, 68990000.00, 62990000.00, 12, N'/images/products/dell-xps-15-9530-oled.png', N'Màn hình 3.5K OLED cảm ứng hiển thị màu sắc chuẩn xác tuyệt đối.', 1),
(54, N'Dell XPS 16 9640', 2, 3, 79990000.00, 74990000.00, 29, N'/images/products/dell-xps-16-9640.png', N'Thiết kế tương lai với hàng phím cảm ứng và bàn rê tàng hình.', 1),
(55, N'Dell XPS 17 9730', 2, 3, 85000000.00, 79900000.00, 10, N'/images/products/dell-xps-17-9730.png', N'Màn hình 17 inch 4K+ khổng lồ phục vụ kỹ sư âm thanh và hình ảnh.', 1),
(56, N'Dell XPS 13 Plus 9320', 2, 4, 46990000.00, 42990000.00, 40, N'/images/products/dell-xps-13-plus-9320.png', N'Kiệt tác công nghệ với bàn phím liền mạch Zero-lattice sang trọng.', 1),
(57, N'Dell XPS 13 9340', 2, 4, 49990000.00, 45990000.00, 23, N'/images/products/dell-xps-13-9340.png', N'Siêu phẩm mỏng nhẹ cao cấp trang bị chip AI Intel mới nhất.', 1),
(58, N'Dell XPS 14 9440', 2, 4, 58990000.00, 53990000.00, 45, N'/images/products/dell-xps-14-9440.png', N'Sự cân bằng hoàn hảo giữa hiệu năng và kích thước 14.5 inch.', 1),
(59, N'Dell Latitude 7440', 2, 4, 33500000.00, 30500000.00, 44, N'/images/products/dell-latitude-7440.png', N'Dòng Latitude cao cấp vỏ nhôm titan nhẹ và siêu bền bỉ.', 1),
(60, N'Dell Latitude 9440 2-in-1', 2, 4, 52000000.00, 48000000.00, 28, N'/images/products/dell-latitude-9440-2-in-1.png', N'Bàn rê chuột Haptic Touchpad thông minh tích hợp phím Zoom.', 1),
(61, N'Dell Vostro 5630', 2, 4, 23990000.00, 20990000.00, 41, N'/images/products/dell-vostro-5630.png', N'Laptop doanh nhân màn hình lớn, vỏ nhôm sang trọng và bảo mật cao.', 1),
(62, N'Dell Precision 3581', 2, 5, 45000000.00, 41500000.00, 17, N'/images/products/dell-precision-3581.png', N'Máy trạm di động chuyên dụng chạy AutoCAD và Revit mượt mà.', 1),
(63, N'Dell Precision 5680', 2, 5, 78000000.00, 72000000.00, 50, N'/images/products/dell-precision-5680.png', N'Thiết kế mỏng nhẹ tựa XPS nhưng mang phần cứng máy trạm ISV.', 1),
(64, N'Dell Precision 7680', 2, 5, 92000000.00, 86000000.00, 9, N'/images/products/dell-precision-7680.png', N'Quái vật đồ họa công nghiệp hỗ trợ nâng cấp RAM CAMM2 tới 128GB.', 1),
(65, N'Dell Precision 7780', 2, 5, 115000000.00, 108000000.00, 7, N'/images/products/dell-precision-7780.png', N'Đỉnh cao máy trạm di động tính toán mô phỏng khoa học chuyên sâu.', 1),
(66, N'Lenovo LOQ 15IAX9 i5', 3, 1, 21990000.00, 18990000.00, 47, N'/images/products/lenovo-loq-15iax9-i5.png', N'Vua gaming tầm trung mới của Lenovo với tản nhiệt buồng kín Hyperchamber.', 1),
(67, N'Lenovo LOQ 15IAX9 i5-4050', 3, 1, 25990000.00, 22990000.00, 19, N'/images/products/lenovo-loq-15iax9-i5-4050.png', N'Card đồ họa RTX 4050 hỗ trợ DLSS 3 chiến mượt game bom tấn.', 1),
(68, N'Lenovo LOQ 15IRX9 i7', 3, 1, 31990000.00, 28490000.00, 23, N'/images/products/lenovo-loq-15irx9-i7.png', N'Cấu hình cực mạnh phân khúc 28 triệu cho game thủ và lập trình viên.', 1),
(69, N'Lenovo Legion 5 16IRX9', 3, 1, 36990000.00, 33490000.00, 10, N'/images/products/lenovo-legion-5-16irx9.png', N'Dòng Legion quốc dân màn hình 2.5K 165Hz chuẩn màu 100% sRGB.', 1),
(70, N'Lenovo Legion Slim 5 16AHP9', 3, 1, 38990000.00, 34990000.00, 19, N'/images/products/lenovo-legion-slim-5-16ahp9.png', N'Chip AI Ryzen Hawk Point tối ưu năng lượng và tản nhiệt Legion ColdFront 5.0.', 1),
(71, N'Lenovo Legion Pro 5 16IRX9', 3, 1, 46990000.00, 42990000.00, 11, N'/images/products/lenovo-legion-pro-5-16irx9.png', N'CPU i9 thế hệ 14 kết hợp màn hình 240Hz 500 nits đỉnh cao.', 1),
(72, N'Lenovo Legion 7 16IRX9', 3, 1, 56990000.00, 51990000.00, 29, N'/images/products/lenovo-legion-7-16irx9.png', N'Vỏ nhôm Glacier White trắng tinh khôi, pin tối đa 99.9Wh.', 1),
(73, N'Lenovo Legion Pro 7 16IRX9H', 3, 1, 84990000.00, 79990000.00, 22, N'/images/products/lenovo-legion-pro-7-16irx9h.png', N'Tản nhiệt buồng hơi Vapor Chamber, chip AI LA-2 tối ưu FPS.', 1),
(74, N'Lenovo Legion 9 16IRX9', 3, 1, 129000000.00, 119900000.00, 34, N'/images/products/lenovo-legion-9-16irx9.png', N'Laptop tản nhiệt nước tích hợp đầu tiên trên thế giới, vỏ Carbon Forged.', 1),
(75, N'Lenovo Legion Go 8APU1', 3, 1, 19990000.00, 17490000.00, 45, N'/images/products/lenovo-legion-go-8apu1.png', N'Máy chơi game cầm tay Windows mạnh mẽ với tay cầm tháo rời.', 1),
(76, N'Lenovo IdeaPad 1 15AMN7', 3, 2, 10990000.00, 8990000.00, 28, N'/images/products/lenovo-ideapad-1-15amn7.png', N'Laptop giá rẻ học online và làm bài tập cho học sinh sinh viên.', 1),
(77, N'Lenovo IdeaPad Slim 3 14IAH8', 3, 2, 14990000.00, 12990000.00, 15, N'/images/products/lenovo-ideapad-slim-3-14iah8.png', N'Chip dòng H hiệu năng cao trong phân khúc 13 triệu.', 1),
(78, N'Lenovo IdeaPad Slim 3 15IRH8', 3, 2, 17490000.00, 14990000.00, 28, N'/images/products/lenovo-ideapad-slim-3-15irh8.png', N'Đa nhiệm khỏe với CPU 10 nhân 16 luồng.', 1),
(79, N'Lenovo IdeaPad Slim 5 14IMH9', 3, 2, 22990000.00, 19990000.00, 27, N'/images/products/lenovo-ideapad-slim-5-14imh9.png', N'Màn hình OLED 2.8K 120Hz sắc nét, vỏ nhôm nguyên khối.', 1),
(80, N'Lenovo IdeaPad Slim 5 16IMH9', 3, 2, 24990000.00, 21990000.00, 18, N'/images/products/lenovo-ideapad-slim-5-16imh9.png', N'Màn hình OLED 16 inch không gian làm việc rộng lớn.', 1),
(81, N'Lenovo ThinkBook 14 G6+ IMH', 3, 2, 23500000.00, 20900000.00, 47, N'/images/products/lenovo-thinkbook-14-g6+-imh.png', N'Dòng máy được sinh viên IT ưa chuộng nhất nhờ cổng kết nối đa dạng.', 1),
(82, N'Lenovo ThinkBook 16 G6+ IRL', 3, 2, 26990000.00, 23990000.00, 22, N'/images/products/lenovo-thinkbook-16-g6+-irl.png', N'Thiết kế 2 tông màu xám đặc trưng, pin lớn 71Wh.', 1),
(83, N'Lenovo ThinkPad E14 Gen 5', 3, 2, 19990000.00, 17490000.00, 49, N'/images/products/lenovo-thinkpad-e14-gen-5.png', N'Bàn phím gõ êm nhất thế giới, độ bền chuẩn ThinkPad.', 1),
(84, N'Lenovo ThinkPad L14 Gen 4', 3, 2, 22500000.00, 19900000.00, 48, N'/images/products/lenovo-thinkpad-l14-gen-4.png', N'Dòng doanh nghiệp bền bỉ, dễ dàng nâng cấp RAM và SSD.', 1),
(85, N'Lenovo Yoga Pro 7 14IMH9', 3, 3, 39990000.00, 35990000.00, 46, N'/images/products/lenovo-yoga-pro-7-14imh9.png', N'Màn hình 3K PureSight Pro chuẩn màu 100% DCI-P3 cho đồ họa.', 1),
(86, N'Lenovo Yoga Pro 9 16IMH9', 3, 3, 59990000.00, 54990000.00, 9, N'/images/products/lenovo-yoga-pro-9-16imh9.png', N'Màn hình Mini-LED 3.2K 165Hz độ sáng 1200 nits đỉnh cao.', 1),
(87, N'Lenovo ThinkPad P14s Gen 4', 3, 3, 36500000.00, 32900000.00, 43, N'/images/products/lenovo-thinkpad-p14s-gen-4.png', N'Máy trạm mỏng nhẹ 1.34kg đạt chuẩn chứng chỉ ISV.', 1),
(88, N'Lenovo ThinkPad P16s Gen 2', 3, 3, 42000000.00, 38500000.00, 45, N'/images/products/lenovo-thinkpad-p16s-gen-2.png', N'Hiệu năng đồ họa mạnh mẽ và tiết kiệm pin vượt trội từ AMD.', 1),
(89, N'Lenovo ThinkBook 16p Gen 4', 3, 3, 49990000.00, 45990000.00, 15, N'/images/products/lenovo-thinkbook-16p-gen-4.png', N'Dòng laptop sáng tạo nội dung có chân cắm phụ kiện Magic Bay.', 1),
(90, N'Lenovo Yoga Slim 7 14IMH9', 3, 4, 30990000.00, 27990000.00, 39, N'/images/products/lenovo-yoga-slim-7-14imh9.png', N'Thiết kế siêu thanh mảnh vỏ nhôm cao cấp, âm thanh Dolby Atmos.', 1),
(91, N'Lenovo Yoga 9 2-in-1 14IMH9', 3, 4, 46990000.00, 42990000.00, 20, N'/images/products/lenovo-yoga-9-2-in-1-14imh9.png', N'Bản lề soundbar Bowers & Wilkins xoay 360 độ độc đáo.', 1),
(92, N'Lenovo ThinkPad X1 Carbon Gen 11', 3, 4, 48990000.00, 44990000.00, 15, N'/images/products/lenovo-thinkpad-x1-carbon-gen-11.png', N'Biểu tượng doanh nhân đỉnh cao với vỏ sợi carbon siêu nhẹ 1.12kg.', 1),
(93, N'Lenovo ThinkPad X1 Carbon Gen 12', 3, 4, 59990000.00, 54990000.00, 34, N'/images/products/lenovo-thinkpad-x1-carbon-gen-12.png', N'Thế hệ thứ 12 với viền siêu mỏng, bàn rê cảm ứng Haptic và chip AI.', 1),
(94, N'Lenovo ThinkPad P1 Gen 6', 3, 5, 76000000.00, 71000000.00, 29, N'/images/products/lenovo-thinkpad-p1-gen-6.png', N'Trạm làm việc di động mạnh nhất trong thân hình mỏng nhẹ 1.78kg.', 1),
(95, N'Lenovo ThinkPad P16 Gen 2', 3, 5, 89000000.00, 83000000.00, 22, N'/images/products/lenovo-thinkpad-p16-gen-2.png', N'Cỗ máy render và tính toán kỹ thuật hạng nặng không đối thủ.', 1),
(96, N'Apple MacBook Air 13 M1 2020', 4, 2, 19990000.00, 17990000.00, 45, N'/images/products/apple-macbook-air-13-m1-2020.png', N'Mẫu MacBook quốc dân giữ giá nhất lịch sử, pin 15 tiếng không quạt gió.', 1),
(97, N'Apple MacBook Air 13 M2 256GB', 4, 4, 26990000.00, 23990000.00, 49, N'/images/products/apple-macbook-air-13-m2-256gb.png', N'Thiết kế tai thỏ vuông vức mới, sạc MagSafe 3 tiện lợi.', 1),
(98, N'Apple MacBook Air 13 M2 16GB', 4, 4, 31990000.00, 28990000.00, 40, N'/images/products/apple-macbook-air-13-m2-16gb.png', N'Phiên bản nâng cấp 16GB RAM cho lập trình viên web và đồ họa 2D.', 1),
(99, N'Apple MacBook Air 15 M2 256GB', 4, 4, 31990000.00, 27990000.00, 19, N'/images/products/apple-macbook-air-15-m2-256gb.png', N'Màn hình 15.3 inch rộng rãi, loa 6 loa có Force-cancelling woofers.', 1),
(100, N'Apple MacBook Air 13 M3 8GB', 4, 4, 27990000.00, 25990000.00, 48, N'/images/products/apple-macbook-air-13-m3-8gb.png', N'Chip M3 tiến trình 3nm hỗ trợ xuất 2 màn hình ngoài khi gập máy.', 1),
(101, N'Apple MacBook Air 13 M3 16GB', 4, 4, 32990000.00, 30490000.00, 25, N'/images/products/apple-macbook-air-13-m3-16gb.png', N'Cấu hình tiêu chuẩn hoàn hảo cho người làm nội dung đa nhiệm.', 1),
(102, N'Apple MacBook Air 15 M3 16GB', 4, 4, 37990000.00, 34990000.00, 8, N'/images/products/apple-macbook-air-15-m3-16gb.png', N'Màn hình Liquid Retina 15 inch rực rỡ, pin bền bỉ 18 tiếng.', 1),
(103, N'Apple MacBook Pro 14 M3 8GB', 4, 3, 39990000.00, 36990000.00, 19, N'/images/products/apple-macbook-pro-14-m3-8gb.png', N'Màn hình Liquid Retina XDR Mini-LED ProMotion 120Hz 1600 nits.', 1),
(104, N'Apple MacBook Pro 14 M3 16GB', 4, 3, 45990000.00, 42990000.00, 7, N'/images/products/apple-macbook-pro-14-m3-16gb.png', N'Ổ cứng 1TB rộng rãi lưu trữ project phim ảnh và mã nguồn.', 1),
(105, N'Apple MacBook Pro 14 M3 Pro 18GB', 4, 3, 49990000.00, 46990000.00, 25, N'/images/products/apple-macbook-pro-14-m3-pro-18gb.png', N'Màu Đen Không Gian Space Black sang trọng chống bám vân tay.', 1),
(106, N'Apple MacBook Pro 14 M3 Pro 36GB', 4, 3, 64990000.00, 60990000.00, 30, N'/images/products/apple-macbook-pro-14-m3-pro-36gb.png', N'36GB Unified Memory chạy mượt Docker, máy ảo và Xcode.', 1),
(107, N'Apple MacBook Pro 14 M3 Max 36GB', 4, 3, 79990000.00, 74990000.00, 22, N'/images/products/apple-macbook-pro-14-m3-max-36gb.png', N'Quái vật đồ họa 3D render Cinema 4D và After Effects siêu tốc.', 1),
(108, N'Apple MacBook Pro 16 M3 Pro 18GB', 4, 3, 64990000.00, 59990000.00, 9, N'/images/products/apple-macbook-pro-16-m3-pro-18gb.png', N'Không gian 16.2 inch khổng lồ, hệ thống 6 loa hay nhất phân khúc.', 1),
(109, N'Apple MacBook Pro 16 M3 Pro 36GB', 4, 3, 74990000.00, 69990000.00, 18, N'/images/products/apple-macbook-pro-16-m3-pro-36gb.png', N'Thời lượng pin lên tới 22 tiếng - lâu nhất trên mọi dòng Mac.', 1),
(110, N'Apple MacBook Pro 16 M3 Max 48GB', 4, 3, 99990000.00, 93990000.00, 41, N'/images/products/apple-macbook-pro-16-m3-max-48gb.png', N'Trùm cuối máy trạm Mac cho các studio sản xuất phim điện ảnh.', 1),
(111, N'Acer Nitro V 15 ANV15-51 i5', 5, 1, 21490000.00, 17990000.00, 50, N'/images/products/acer-nitro-v-15-anv15-51-i5.png', N'Laptop gaming bán chạy nhất phân khúc dưới 18 triệu.', 1),
(112, N'Acer Nitro V 15 ANV15-51 i5-4050', 5, 1, 24990000.00, 21490000.00, 25, N'/images/products/acer-nitro-v-15-anv15-51-i5-4050.png', N'Card RTX 4050 giá mềm nhất phân khúc sinh viên.', 1),
(113, N'Acer Nitro V 15 ANV15-51 i7', 5, 1, 28990000.00, 24990000.00, 18, N'/images/products/acer-nitro-v-15-anv15-51-i7.png', N'CPU i7 10 nhân cân tốt cả game và đồ họa 3D cơ bản.', 1),
(114, N'Acer Nitro V 16 ANV16-41', 5, 1, 29990000.00, 26990000.00, 46, N'/images/products/acer-nitro-v-16-anv16-41.png', N'Màn hình 16 inch 16:10 165Hz chuẩn gaming thế hệ mới.', 1),
(115, N'Acer Nitro 5 Tiger AN515-58 i5', 5, 1, 22990000.00, 19490000.00, 36, N'/images/products/acer-nitro-5-tiger-an515-58-i5.png', N'Thiết kế hầm hố với 2 cổng thoát nhiệt phía sau dạng phi thuyền.', 1),
(116, N'Acer Nitro 5 Tiger AN515-58 i7', 5, 1, 27990000.00, 23990000.00, 30, N'/images/products/acer-nitro-5-tiger-an515-58-i7.png', N'CPU 14 nhân 20 luồng cho khả năng render và biên tập video ấn tượng.', 1),
(117, N'Acer Nitro 16 Phoenix AN16-41', 5, 1, 35990000.00, 31990000.00, 46, N'/images/products/acer-nitro-16-phoenix-an16-41.png', N'Kim loại lỏng tản nhiệt, màn hình 2K+ 165Hz sRGB 100%.', 1),
(118, N'Acer Nitro 17 AN17-51', 5, 1, 39990000.00, 35990000.00, 34, N'/images/products/acer-nitro-17-an17-51.png', N'Màn hình lớn 17.3 inch trải nghiệm không gian game bao quát.', 1),
(119, N'Acer Predator Helios Neo 16 PHN16-71', 5, 1, 41990000.00, 37990000.00, 14, N'/images/products/acer-predator-helios-neo-16-phn16-71.png', N'Khắc laser mật mã bí ẩn trên nắp A, quạt tản nhiệt AeroBlade 3D thế hệ 5.', 1),
(120, N'Acer Predator Helios Neo 16 PHN16-72', 5, 1, 46990000.00, 42990000.00, 21, N'/images/products/acer-predator-helios-neo-16-phn16-72.png', N'Cấu hình i7 thế hệ 14 HX cùng màn hình WQXGA 240Hz siêu mượt.', 1),
(121, N'Acer Predator Helios 16 PH16-72', 5, 1, 65990000.00, 59990000.00, 13, N'/images/products/acer-predator-helios-16-ph16-72.png', N'Dòng gaming Predator đầu bảng với màn hình Mini-LED 250Hz.', 1),
(122, N'Acer Predator Helios 18 PH18-72', 5, 1, 109000000.00, 99900000.00, 20, N'/images/products/acer-predator-helios-18-ph18-72.png', N'Quái thú gaming 18 inch trang bị card đồ họa mạnh nhất thế giới.', 1),
(123, N'Acer Aspire 3 A315-59 i3', 5, 2, 10990000.00, 8990000.00, 40, N'/images/products/acer-aspire-3-a315-59-i3.png', N'Laptop giá siêu rẻ cho học sinh học Zoom và làm Word.', 1),
(124, N'Acer Aspire 3 A315-59 i5', 5, 2, 13990000.00, 11990000.00, 39, N'/images/products/acer-aspire-3-a315-59-i5.png', N'Nâng cấp sẵn 16GB RAM chạy đa nhiệm văn phòng mượt mà.', 1),
(125, N'Acer Aspire 5 A515-58M', 5, 2, 16990000.00, 14490000.00, 21, N'/images/products/acer-aspire-5-a515-58m.png', N'Vỏ nắp nhôm sang trọng, có công nghệ lọc tiếng ồn PurifiedVoice.', 1),
(126, N'Acer Aspire 7 Gaming A715-76G', 5, 2, 18990000.00, 15990000.00, 42, N'/images/products/acer-aspire-7-gaming-a715-76g.png', N'Thiết kế văn phòng tối giản nhưng ẩn chứa cấu hình gaming.', 1),
(127, N'Acer Swift Go 14 SFG14-71', 5, 2, 22990000.00, 19990000.00, 32, N'/images/products/acer-swift-go-14-sfg14-71.png', N'Màn hình OLED 2.8K siêu đẹp, camera 1440p QHD rõ nét.', 1),
(128, N'Acer Swift Go 14 SFG14-73 AI', 5, 2, 26990000.00, 23990000.00, 42, N'/images/products/acer-swift-go-14-sfg14-73-ai.png', N'Tích hợp phím Copilot và bàn rê chuột phát sáng đa phương tiện.', 1),
(129, N'Acer Swift Lite 14 SFL14-51M', 5, 2, 15990000.00, 13490000.00, 30, N'/images/products/acer-swift-lite-14-sfl14-51m.png', N'Trọng lượng nhẹ 1.27kg, màn hình IPS chống chói.', 1),
(130, N'Acer Swift X 14 SFX14-71G', 5, 3, 34990000.00, 30990000.00, 28, N'/images/products/acer-swift-x-14-sfx14-71g.png', N'Laptop đồ họa 14.5 inch OLED chuẩn màu Delta E < 2 có card RTX.', 1),
(131, N'Acer Swift Edge 16 SFE16-43', 5, 4, 33990000.00, 29990000.00, 19, N'/images/products/acer-swift-edge-16-sfe16-43.png', N'Laptop 16 inch OLED nhẹ nhất thế giới vỏ hợp kim Magie-Nhôm chỉ 1.23kg.', 1),
(132, N'Acer Swift 3 SF314-512', 5, 4, 18990000.00, 16490000.00, 13, N'/images/products/acer-swift-3-sf314-512.png', N'Thiết kế thanh thoát, chuẩn Intel Evo sạc nhanh 30 phút dùng 4 tiếng.', 1),
(133, N'Acer Swift Go 16 SFG16-72', 5, 4, 29990000.00, 26990000.00, 37, N'/images/products/acer-swift-go-16-sfg16-72.png', N'Bản màn hình lớn OLED 3.2K kèm RAM khủng 32GB.', 1),
(134, N'Acer Swift X 16 SFX16-61G', 5, 3, 39990000.00, 35990000.00, 36, N'/images/products/acer-swift-x-16-sfx16-61g.png', N'Màn hình 3.2K OLED 120Hz sRGB 100% chuyên dựng video 4K.', 1),
(135, N'Acer TravelMate P2 TMP214-55', 5, 2, 16500000.00, 14500000.00, 10, N'/images/products/acer-travelmate-p2-tmp214-55.png', N'Dòng doanh nghiệp bền bỉ, bản lề mở phẳng 180 độ.', 1),
(136, N'HP Victus 15-fb1013AX', 6, 1, 19990000.00, 16990000.00, 8, N'/images/products/hp-victus-15-fb1013ax.png', N'Laptop gaming HP thiết kế tinh tế lịch sự như văn phòng.', 1),
(137, N'HP Victus 15-fa1139TX', 6, 1, 23990000.00, 20490000.00, 12, N'/images/products/hp-victus-15-fa1139tx.png', N'Logo chữ V phản quang, bàn phím số phụ tiện thao tác.', 1),
(138, N'HP Victus 15-fa1155TX', 6, 1, 27990000.00, 23990000.00, 14, N'/images/products/hp-victus-15-fa1155tx.png', N'Trang bị RTX 4050 và pin 70Wh cho thời lượng chơi game ấn tượng.', 1),
(139, N'HP Victus 16-r0129TX', 6, 1, 33990000.00, 29990000.00, 45, N'/images/products/hp-victus-16-r0129tx.png', N'Màn hình 16.1 inch 165Hz chuẩn màu sRGB 100%.', 1),
(140, N'HP Victus 16-s0078AX', 6, 1, 31990000.00, 27990000.00, 15, N'/images/products/hp-victus-16-s0078ax.png', N'Cấu hình AMD mạnh mẽ, tản nhiệt buồng gió kép cải tiến.', 1),
(141, N'HP OMEN 16-wd0014TX', 6, 1, 41990000.00, 36990000.00, 48, N'/images/products/hp-omen-16-wd0014tx.png', N'Dòng gaming cao cấp OMEN với công nghệ âm thanh Bang & Olufsen.', 1),
(142, N'HP OMEN 16-wf0043TX', 6, 1, 56990000.00, 50990000.00, 32, N'/images/products/hp-omen-16-wf0043tx.png', N'Màn hình 2K 240Hz, quạt Tempest Cooling tản nhiệt cực êm.', 1),
(143, N'HP OMEN Transcend 14-fb0063TX', 6, 1, 49990000.00, 44990000.00, 43, N'/images/products/hp-omen-transcend-14-fb0063tx.png', N'Laptop gaming 14 inch OLED mỏng nhẹ nhất thế giới của HP.', 1),
(144, N'HP 14s-em0080AU', 6, 2, 11490000.00, 9490000.00, 9, N'/images/products/hp-14s-em0080au.png', N'Thiết kế nhỏ gọn, màu bạc trang nhã cho sinh viên.', 1),
(145, N'HP 15s-fq5229TU', 6, 2, 13990000.00, 11790000.00, 29, N'/images/products/hp-15s-fq5229tu.png', N'Màn hình lớn viền mỏng có bàn phím số đầy đủ.', 1),
(146, N'HP 15s-fq5228TU i5', 6, 2, 16990000.00, 14490000.00, 29, N'/images/products/hp-15s-fq5228tu-i5.png', N'Bản 16GB RAM nâng cao hiệu suất làm việc văn phòng đa nhiệm.', 1),
(147, N'HP Pavilion 14-dv2073TU', 6, 2, 17490000.00, 14990000.00, 43, N'/images/products/hp-pavilion-14-dv2073tu.png', N'Vỏ nhôm màu vàng Gold sang trọng, loa B&O sống động.', 1),
(148, N'HP Pavilion 15-eg3093TU', 6, 2, 19490000.00, 16990000.00, 34, N'/images/products/hp-pavilion-15-eg3093tu.png', N'Chip Intel gen 13 đời mới, thiết kế bo cong mềm mại.', 1),
(149, N'HP Pavilion 15-eg3091TU i7', 6, 2, 23990000.00, 20990000.00, 38, N'/images/products/hp-pavilion-15-eg3091tu-i7.png', N'Phiên bản i7 mạnh mẽ, đa nhiệm không giật lag.', 1),
(150, N'HP Pavilion Plus 14-ew0015TU', 6, 2, 26990000.00, 23490000.00, 21, N'/images/products/hp-pavilion-plus-14-ew0015tu.png', N'Màn hình OLED 2.8K 120Hz chuẩn màu, vỏ nhôm nguyên khối.', 1),
(151, N'HP ProBook 440 G10', 6, 2, 18500000.00, 16500000.00, 40, N'/images/products/hp-probook-440-g10.png', N'Dòng laptop doanh nghiệp có độ bền tiêu chuẩn quân đội MIL-STD 810H.', 1),
(152, N'HP ProBook 450 G10', 6, 2, 21900000.00, 19500000.00, 5, N'/images/products/hp-probook-450-g10.png', N'Bảo mật vân tay và chip mã hóa dữ liệu TPM 2.0.', 1),
(153, N'HP Envy x360 14-es0013DX', 6, 4, 22990000.00, 19990000.00, 48, N'/images/products/hp-envy-x360-14-es0013dx.png', N'Xoay gập 360 độ màn hình cảm ứng, thiết kế nhôm cao cấp.', 1),
(154, N'HP Envy x360 14-fc0085TU AI', 6, 4, 29990000.00, 26990000.00, 12, N'/images/products/hp-envy-x360-14-fc0085tu-ai.png', N'Laptop AI 2 trong 1 trang bị màn hình OLED 2.8K 120Hz rực rỡ.', 1),
(155, N'HP Envy 16-h1023TX', 6, 3, 49990000.00, 44990000.00, 48, N'/images/products/hp-envy-16-h1023tx.png', N'Laptop sáng tạo nội dung màn hình cảm ứng 16 inch 120Hz.', 1),
(156, N'HP Spectre x360 14-eu0051TU', 6, 4, 49990000.00, 45990000.00, 39, N'/images/products/hp-spectre-x360-14-eu0051tu.png', N'Tuyệt tác vát cạnh kim cương sang trọng nhất của HP, màn OLED 2.8K.', 1),
(157, N'HP Spectre x360 16-f2013DX', 6, 4, 55000000.00, 49900000.00, 22, N'/images/products/hp-spectre-x360-16-f2013dx.png', N'Màn hình cảm ứng 4K UHD+ OLED xoay gập 360 độ cực đại.', 1),
(158, N'HP EliteBook 840 G10', 6, 4, 33500000.00, 29900000.00, 46, N'/images/products/hp-elitebook-840-g10.png', N'Dòng doanh nhân cao cấp với màn hình chống nhìn trộm HP Sure View.', 1),
(159, N'HP EliteBook 1040 G10', 6, 4, 45000000.00, 41000000.00, 26, N'/images/products/hp-elitebook-1040-g10.png', N'Trọng lượng nhẹ 1.18kg, thời lượng pin cả ngày dài.', 1),
(160, N'HP ZBook Power G10', 6, 5, 52000000.00, 47500000.00, 12, N'/images/products/hp-zbook-power-g10.png', N'Máy trạm di động chuyên đồ họa 3D kiến trúc và Render V-Ray.', 1),
(161, N'MSI Thin 15 B12UCX', 7, 1, 16990000.00, 14490000.00, 23, N'/images/products/msi-thin-15-b12ucx.png', N'Laptop gaming nhẹ nhất phân khúc chỉ 1.86kg.', 1),
(162, N'MSI Thin 15 B13UC', 7, 1, 19990000.00, 16990000.00, 32, N'/images/products/msi-thin-15-b13uc.png', N'Nâng cấp CPU gen 13 và card RTX 3050 chiến mượt CS2, Valorant.', 1),
(163, N'MSI Thin A15 B7UC', 7, 1, 18990000.00, 16290000.00, 15, N'/images/products/msi-thin-a15-b7uc.png', N'Phiên bản chip AMD tiết kiệm pin kết hợp phím LED xanh neon.', 1),
(164, N'MSI Cyborg 15 A12VE', 7, 1, 23990000.00, 19990000.00, 34, N'/images/products/msi-cyborg-15-a12ve.png', N'Thiết kế vỏ bán trong suốt xuyên thấu phong cách Cyberpunk độc lạ.', 1),
(165, N'MSI Cyborg 15 A13VFK', 7, 1, 28990000.00, 24990000.00, 5, N'/images/products/msi-cyborg-15-a13vfk.png', N'Trang bị card RTX 4060 8GB mức giá rẻ hàng đầu thị trường.', 1),
(166, N'MSI Katana 15 B13VFK', 7, 1, 32990000.00, 28990000.00, 21, N'/images/products/msi-katana-15-b13vfk.png', N'Bàn phím RGB 4 vùng phím WASD nổi bật, tản nhiệt Cooler Boost 5.', 1),
(167, N'MSI Sword 16 HX B14VFKG', 7, 1, 37990000.00, 33990000.00, 37, N'/images/products/msi-sword-16-hx-b14vfkg.png', N'Màn hình 16:10 FHD+ 144Hz kèm vi xử lý 20 nhân 28 luồng.', 1),
(168, N'MSI Vector 16 HX A13VHG', 7, 1, 56990000.00, 51990000.00, 16, N'/images/products/msi-vector-16-hx-a13vhg.png', N'Tối ưu hiệu năng OverBoost 240W cho game thủ eSports chuyên nghiệp.', 1),
(169, N'MSI Raider GE78 HX 14VHG', 7, 1, 89990000.00, 83990000.00, 37, N'/images/products/msi-raider-ge78-hx-14vhg.png', N'Dải đèn Matrix Light Bar ma trận phía trước tuyệt đẹp.', 1),
(170, N'MSI Titan 18 HX A14VIG', 7, 1, 149000000.00, 139900000.00, 11, N'/images/products/msi-titan-18-hx-a14vig.png', N'Cỗ máy đắt đỏ và mạnh nhất thế giới với 128GB RAM và màn 4K Mini-LED.', 1),
(171, N'MSI Stealth 14 Studio A13VE', 7, 1, 45990000.00, 40990000.00, 45, N'/images/products/msi-stealth-14-studio-a13ve.png', N'Laptop gaming 14 inch vỏ Magie-Nhôm đạt chuẩn NVIDIA Studio.', 1),
(172, N'MSI Stealth 16 AI Studio A1VGG', 7, 1, 65990000.00, 59990000.00, 24, N'/images/products/msi-stealth-16-ai-studio-a1vgg.png', N'Màn hình OLED 240Hz siêu nhẹ dưới 2kg mang theo dễ dàng.', 1),
(173, N'MSI Modern 14 C12MO', 7, 2, 12990000.00, 10490000.00, 45, N'/images/products/msi-modern-14-c12mo.png', N'Thiết kế mỏng nhẹ màu đen lịch lãm cho sinh viên văn phòng.', 1),
(174, N'MSI Modern 14 C13M i5', 7, 2, 15990000.00, 13490000.00, 37, N'/images/products/msi-modern-14-c13m-i5.png', N'Bản 16GB RAM mở nắp 180 độ, sạc Type-C tiện dụng.', 1),
(175, N'MSI Modern 15 B13M i7', 7, 2, 19990000.00, 16990000.00, 43, N'/images/products/msi-modern-15-b13m-i7.png', N'Màn hình 15.6 inch bàn phím số, CPU 10 nhân xử lý số liệu nhanh.', 1),
(176, N'MSI Prestige 13 AI Evo A1MG', 7, 4, 32990000.00, 29990000.00, 17, N'/images/products/msi-prestige-13-ai-evo-a1mg.png', N'Trọng lượng siêu thực chỉ 990g, màn hình OLED 2.8K chuẩn màu.', 1),
(177, N'MSI Prestige 16 AI Evo B1MG', 7, 4, 38990000.00, 34990000.00, 14, N'/images/products/msi-prestige-16-ai-evo-b1mg.png', N'Pin tối đa chuẩn bay 99.9Wh trong thân hình chỉ 1.5kg.', 1),
(178, N'MSI Summit E14 Flip Evo A13MT', 7, 4, 35000000.00, 31000000.00, 28, N'/images/products/msi-summit-e14-flip-evo-a13mt.png', N'Laptop doanh nhân 2 trong 1 bảo mật vân tay và Tobii Aware.', 1),
(179, N'MSI Creator Z17 HX Studio A13VFT', 7, 3, 68000000.00, 62000000.00, 15, N'/images/products/msi-creator-z17-hx-studio-a13vft.png', N'Màn hình cảm ứng 17 inch QHD+ 165Hz hỗ trợ bút vẽ MSI Pen 2.', 1),
(180, N'MSI Creator M16 B13VE', 7, 3, 34990000.00, 30990000.00, 39, N'/images/products/msi-creator-m16-b13ve.png', N'Dòng Creator tầm trung tối ưu cho phần mềm đồ họa Adobe.', 1),
(181, N'LG gram 14Z90S 2024', 8, 4, 31990000.00, 28990000.00, 38, N'/images/products/lg-gram-14z90s-2024.png', N'Vua thời lượng pin và trọng lượng siêu nhẹ 1.12kg chuẩn quân đội.', 1),
(182, N'LG gram 15Z90S 2024', 8, 4, 34990000.00, 31490000.00, 5, N'/images/products/lg-gram-15z90s-2024.png', N'Màn hình 15.6 inch chống chói, bàn phím số, pin dùng cả ngày.', 1),
(183, N'LG gram 16Z90S 2024', 8, 4, 39990000.00, 35990000.00, 43, N'/images/products/lg-gram-16z90s-2024.png', N'Kích thước 16 inch nhưng chỉ nặng 1.19kg, độ phân giải 2.5K.', 1),
(184, N'LG gram 17Z90S 2024', 8, 4, 45990000.00, 41990000.00, 25, N'/images/products/lg-gram-17z90s-2024.png', N'Laptop 17 inch nhẹ nhất thế giới vỏ Magie Carbon chỉ 1.35kg.', 1),
(185, N'LG gram Pro 16Z90SP RTX 3050', 8, 3, 52990000.00, 47990000.00, 36, N'/images/products/lg-gram-pro-16z90sp-rtx-3050.png', N'Laptop có card đồ họa rời nhẹ nhất thế giới chỉ 1.28kg màn 144Hz.', 1),
(186, N'LG gram Pro 16 2-in-1 OLED', 8, 4, 56990000.00, 51990000.00, 6, N'/images/products/lg-gram-pro-16-2-in-1-oled.png', N'Kỷ lục Guiness laptop 16 inch 2 trong 1 nhẹ nhất thế giới màn OLED.', 1),
(187, N'LG gram Style 14Z90RS', 8, 4, 38990000.00, 34990000.00, 12, N'/images/products/lg-gram-style-14z90rs.png', N'Mặt lưng đổi màu theo góc nhìn, bàn rê chuột cảm ứng tàng hình ẩn LED.', 1),
(188, N'LG gram SuperSlim 15Z90RT', 8, 4, 39990000.00, 35990000.00, 28, N'/images/products/lg-gram-superslim-15z90rt.png', N'Laptop mỏng nhất lịch sử LG chỉ 10.9mm nặng đúng 990 gram.', 1),
(189, N'Gigabyte G5 MF5-52VN353SH', 9, 1, 20990000.00, 18490000.00, 24, N'/images/products/gigabyte-g5-mf5-52vn353sh.png', N'Vua p/p cấu hình chip H kết hợp RTX 4050 dưới 19 triệu.', 1),
(190, N'Gigabyte G5 KF5-53VN353SH', 9, 1, 25990000.00, 22490000.00, 20, N'/images/products/gigabyte-g5-kf5-53vn353sh.png', N'Trang bị RTX 4060 8GB chiến game AAA và làm kỹ xảo đồ họa.', 1),
(191, N'Gigabyte G6 KF-H3VN853SH', 9, 1, 28990000.00, 25990000.00, 8, N'/images/products/gigabyte-g6-kf-h3vn853sh.png', N'Màn hình 16 inch 16:10 165Hz viền mỏng không gian game rộng.', 1),
(192, N'Gigabyte AORUS 15 BKF', 9, 1, 39990000.00, 35990000.00, 20, N'/images/products/gigabyte-aorus-15-bkf.png', N'Dòng gaming Aorus cao cấp, màn hình QHD 165Hz chuẩn sRGB 100%.', 1),
(193, N'Gigabyte AORUS 16X AKG', 9, 1, 52990000.00, 47990000.00, 41, N'/images/products/gigabyte-aorus-16x-akg.png', N'Công nghệ tản nhiệt WINDFORCE Infinity với quạt kép 84 cánh.', 1),
(194, N'Gigabyte AERO 14 OLED BMF', 9, 3, 44990000.00, 39990000.00, 10, N'/images/products/gigabyte-aero-14-oled-bmf.png', N'Màn hình OLED 2.8K chứng nhận màu sắc X-Rite và Pantone chuẩn quốc tế.', 1),
(195, N'Microsoft Surface Laptop 5 13.5 i5', 10, 4, 25990000.00, 22990000.00, 10, N'/images/products/microsoft-surface-laptop-5-13.5-i5.png', N'Vỏ nhôm nguyên khối bo cong quyến rũ, màn hình cảm ứng PixelSense 3:2.', 1),
(196, N'Microsoft Surface Laptop 5 13.5 i7', 10, 4, 34990000.00, 30990000.00, 36, N'/images/products/microsoft-surface-laptop-5-13.5-i7.png', N'Phiên bản 16GB RAM màu Sage Xanh Rêu độc đáo và sang trọng.', 1),
(197, N'Microsoft Surface Laptop 5 15 i7', 10, 4, 39990000.00, 35990000.00, 9, N'/images/products/microsoft-surface-laptop-5-15-i7.png', N'Màn hình 15 inch siêu sắc nét, gõ phím êm ái hàng đầu.', 1),
(198, N'Microsoft Surface Laptop 6 13.5 AI', 10, 4, 38990000.00, 35490000.00, 39, N'/images/products/microsoft-surface-laptop-6-13.5-ai.png', N'Dòng Surface thế hệ mới nhất tích hợp chip NPU xử lý AI siêu tốc.', 1),
(199, N'Microsoft Surface Pro 9 i5', 10, 4, 26990000.00, 23990000.00, 13, N'/images/products/microsoft-surface-pro-9-i5.png', N'Máy tính 2 trong 1 linh hoạt nhất thế giới kèm chân đế kickstand.', 1),
(200, N'Microsoft Surface Laptop Studio 2', 10, 3, 69990000.00, 63990000.00, 13, N'/images/products/microsoft-surface-laptop-studio-2.png', N'Màn hình kéo gập 3 chế độ sáng tạo độc nhất vô nhị dành cho Designer.', 1);

GO

SET IDENTITY_INSERT Products OFF;
GO

-- =============================================
-- ProductSpecifications
-- =============================================
-- 4. ProductSpecifications
INSERT INTO ProductSpecifications (ProductId, CPU, RamGB, StorageGB, StorageType, GPU, ScreenSizeInch, RefreshRateHz, WeightKg, BatteryWh, OS) VALUES
(1, N'Intel Core i5-12500H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3050 4GB', 15.6, 144, 2.2, 56, N'Windows 11 Home'),
(2, N'AMD Ryzen 5 7535HS', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3050 4GB', 15.6, 144, 2.3, 48, N'Windows 11 Home'),
(3, N'AMD Ryzen 7 7735HS', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 15.6, 144, 2.2, 90, N'Windows 11 Home'),
(4, N'Intel Core i7-13650HX', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 16.0, 165, 2.27, 90, N'Windows 11 Home'),
(5, N'Intel Core i7-13650HX', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 16.0, 165, 2.5, 90, N'Windows 11 Home'),
(6, N'Intel Core i9-14900HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4080 12GB', 16.0, 240, 2.5, 90, N'Windows 11 Home'),
(7, N'Intel Core i9-14900HX', 32, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4080 12GB', 16.0, 240, 2.65, 90, N'Windows 11 Home'),
(8, N'Intel Core i9-14900HX', 64, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4090 16GB', 18.0, 240, 3.1, 90, N'Windows 11 Home'),
(9, N'AMD Ryzen 9 8945HS', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 14.0, 120, 1.5, 73, N'Windows 11 Home'),
(10, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 240, 1.85, 90, N'Windows 11 Home'),
(11, N'Intel Core i9-13900H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 13.4, 165, 1.18, 56, N'Windows 11 Home'),
(12, N'Intel Core i9-13900H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.0, 240, 2.1, 90, N'Windows 11 Home'),
(13, N'AMD Ryzen 5 7520U', 16, 512, N'SSD NVMe PCIe 3.0', N'AMD Radeon 610M', 14.0, 60, 1.38, 42, N'Windows 11 Home'),
(14, N'AMD Ryzen 5 7520U', 16, 512, N'SSD NVMe PCIe 3.0', N'AMD Radeon 610M', 15.6, 60, 1.63, 42, N'Windows 11 Home'),
(15, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.4, 42, N'Windows 11 Home'),
(16, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 15.6, 60, 1.7, 42, N'Windows 11 Home'),
(17, N'Intel Core i7-13700H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 16.0, 60, 1.88, 42, N'Windows 11 Home'),
(18, N'Intel Core Ultra 5 125H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 120, 1.3, 75, N'Windows 11 Home'),
(19, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 16.0, 120, 1.5, 75, N'Windows 11 Home'),
(20, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.49, 42, N'Windows 11 Pro'),
(21, N'Intel Core i7-1355U', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 90, 0.99, 63, N'Windows 11 Pro'),
(22, N'Intel Core Ultra 7 155H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 15.6, 120, 1.8, 75, N'Windows 11 Home'),
(23, N'Intel Core i9-13980HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 120, 1.9, 90, N'Windows 11 Home'),
(24, N'Intel Core i9-13980HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 120, 2.4, 90, N'Windows 11 Pro'),
(25, N'AMD Ryzen AI 9 HX 370', 64, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 60, 1.85, 90, N'Windows 11 Pro'),
(26, N'AMD Ryzen AI 9 365', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 13.3, 60, 1.38, 73, N'Windows 11 Pro'),
(27, N'Intel Core i9-13900H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 14.5, 120, 1.65, 76, N'Windows 11 Home'),
(28, N'Intel Core i7-12700H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3060 6GB', 15.6, 60, 2.34, 92, N'Windows 11 Pro'),
(29, N'Intel Core Ultra 5 125H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 120, 1.2, 75, N'Windows 11 Home'),
(30, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 120, 1.2, 75, N'Windows 11 Home'),
(31, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 13.3, 60, 1.0, 63, N'Windows 11 Home'),
(32, N'AMD Ryzen AI 9 365', 32, 1024, N'SSD NVMe PCIe 4.0', N'AMD Radeon 880M', 16.0, 120, 1.5, 78, N'Windows 11 Home'),
(33, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 120, 1.65, 75, N'Windows 11 Home'),
(34, N'Intel Core i7-1360P', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 90, 1.5, 75, N'Windows 11 Home'),
(35, N'Intel Core Ultra 5 125H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 60, 1.29, 63, N'Windows 11 Pro'),
(36, N'Intel Core i5-13450HX', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3050 6GB', 15.6, 120, 2.65, 56, N'Windows 11 Home'),
(37, N'Intel Core i7-13650HX', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 15.6, 165, 2.65, 86, N'Windows 11 Home'),
(38, N'Intel Core i9-13900HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 240, 2.87, 86, N'Windows 11 Home'),
(39, N'Intel Core Ultra 7 155H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 240, 2.61, 90, N'Windows 11 Home'),
(40, N'Intel Core Ultra 9 185H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4080 12GB', 16.0, 240, 2.72, 90, N'Windows 11 Home'),
(41, N'Intel Core i9-14900HX', 64, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4090 16GB', 18.0, 165, 4.04, 97, N'Windows 11 Home'),
(42, N'Intel Core i3-1215U', 8, 512, N'SSD NVMe PCIe', N'Intel UHD Graphics', 15.6, 120, 1.65, 41, N'Windows 11 Home'),
(43, N'Intel Core i5-1235U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 15.6, 120, 1.65, 41, N'Windows 11 Home'),
(44, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.55, 54, N'Windows 11 Home'),
(45, N'Intel Core i7-1360P', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 16.0, 60, 1.85, 54, N'Windows 11 Home'),
(46, N'Intel Core 5 120U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Graphics', 14.0, 60, 1.57, 54, N'Windows 11 Home'),
(47, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 14.0, 60, 1.48, 41, N'Windows 11 Home'),
(48, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 15.6, 120, 1.66, 41, N'Windows 11 Home'),
(49, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.54, 54, N'Windows 11 Pro'),
(50, N'Intel Core i7-1355U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.39, 54, N'Windows 11 Pro'),
(51, N'Intel Core i7-13700H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.0, 120, 2.06, 86, N'Windows 11 Home'),
(52, N'Intel Core i7-13700H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 15.6, 60, 1.86, 86, N'Windows 11 Home'),
(53, N'Intel Core i9-13900H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 15.6, 60, 1.92, 86, N'Windows 11 Home'),
(54, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.3, 90, 2.13, 99, N'Windows 11 Home'),
(55, N'Intel Core i9-13900H', 64, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4080 12GB', 17.0, 60, 2.44, 97, N'Windows 11 Pro'),
(56, N'Intel Core i7-1360P', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 13.4, 60, 1.26, 55, N'Windows 11 Home'),
(57, N'Intel Core Ultra 7 155H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 13.4, 120, 1.19, 55, N'Windows 11 Home'),
(58, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 14.5, 120, 1.68, 69, N'Windows 11 Home'),
(59, N'Intel Core i7-1365U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.33, 57, N'Windows 11 Pro'),
(60, N'Intel Core i7-1365U', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.53, 60, N'Windows 11 Pro'),
(61, N'Intel Core i7-1360P', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 16.0, 60, 1.8, 54, N'Windows 11 Pro'),
(62, N'Intel Core i7-13800H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA RTX A1000 6GB', 15.6, 60, 1.8, 64, N'Windows 11 Pro'),
(63, N'Intel Core i9-13900H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA RTX 2000 Ada 8GB', 16.0, 60, 1.91, 66, N'Windows 11 Pro'),
(64, N'Intel Core i9-13950HX', 64, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA RTX 3500 Ada 12GB', 16.0, 60, 2.6, 93, N'Windows 11 Pro'),
(65, N'Intel Core i9-13950HX', 64, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA RTX 5000 Ada 16GB', 17.3, 120, 3.02, 93, N'Windows 11 Pro'),
(66, N'Intel Core i5-12450HX', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3050 6GB', 15.6, 144, 2.38, 60, N'Windows 11 Home'),
(67, N'Intel Core i5-12450HX', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 15.6, 144, 2.38, 60, N'Windows 11 Home'),
(68, N'Intel Core i7-13650HX', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 15.6, 144, 2.38, 60, N'Windows 11 Home'),
(69, N'Intel Core i7-14650HX', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.0, 165, 2.3, 80, N'Windows 11 Home'),
(70, N'AMD Ryzen 7 8845HS', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.0, 165, 2.3, 80, N'Windows 11 Home'),
(71, N'Intel Core i9-14900HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 240, 2.5, 80, N'Windows 11 Home'),
(72, N'Intel Core i9-14900HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 240, 2.24, 99, N'Windows 11 Home'),
(73, N'Intel Core i9-14900HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4080 12GB', 16.0, 240, 2.62, 99, N'Windows 11 Home'),
(74, N'Intel Core i9-14900HX', 64, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4090 16GB', 16.0, 165, 2.56, 99, N'Windows 11 Home'),
(75, N'AMD Ryzen Z1 Extreme', 16, 512, N'SSD NVMe PCIe 4.0', N'AMD Radeon 780M', 8.8, 144, 0.85, 49, N'Windows 11 Home'),
(76, N'AMD Ryzen 3 7320U', 8, 256, N'SSD NVMe PCIe', N'AMD Radeon 610M', 15.6, 60, 1.58, 42, N'Windows 11 Home'),
(77, N'Intel Core i5-12450H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel UHD Graphics', 14.0, 60, 1.37, 47, N'Windows 11 Home'),
(78, N'Intel Core i7-13620H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel UHD Graphics', 15.6, 60, 1.62, 47, N'Windows 11 Home'),
(79, N'Intel Core Ultra 5 125H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 120, 1.46, 57, N'Windows 11 Home'),
(80, N'Intel Core Ultra 7 155H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 16.0, 120, 1.82, 57, N'Windows 11 Home'),
(81, N'Intel Core Ultra 5 125H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 90, 1.38, 60, N'Windows 11 Home'),
(82, N'Intel Core i7-13700H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 16.0, 120, 1.8, 71, N'Windows 11 Home'),
(83, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.41, 47, N'Windows 11 Home'),
(84, N'AMD Ryzen 5 PRO 7530U', 16, 512, N'SSD NVMe PCIe', N'AMD Radeon Graphics', 14.0, 60, 1.4, 57, N'Windows 11 Pro'),
(85, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 14.5, 120, 1.59, 73, N'Windows 11 Home'),
(86, N'Intel Core Ultra 9 185H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 165, 2.23, 84, N'Windows 11 Home'),
(87, N'Intel Core i7-1370P', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA RTX A500 4GB', 14.0, 60, 1.34, 52, N'Windows 11 Pro'),
(88, N'AMD Ryzen 7 PRO 7840U', 32, 1024, N'SSD NVMe PCIe 4.0', N'AMD Radeon 780M', 16.0, 60, 1.7, 86, N'Windows 11 Pro'),
(89, N'Intel Core i9-13900H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.0, 165, 2.2, 80, N'Windows 11 Home'),
(90, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 120, 1.39, 65, N'Windows 11 Home'),
(91, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 120, 1.35, 75, N'Windows 11 Home'),
(92, N'Intel Core i7-1365U', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.12, 57, N'Windows 11 Pro'),
(93, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 120, 1.09, 57, N'Windows 11 Pro'),
(94, N'Intel Core i7-13800H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA RTX 4080 12GB', 16.0, 165, 1.78, 90, N'Windows 11 Pro'),
(95, N'Intel Core i9-13980HX', 64, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA RTX 3500 Ada 12GB', 16.0, 60, 2.95, 94, N'Windows 11 Pro'),
(96, N'Apple M1 8-core CPU', 8, 256, N'Apple SSD Onboard', N'Apple M1 7-core GPU', 13.3, 60, 1.29, 49, N'macOS Sonoma'),
(97, N'Apple M2 8-core CPU', 8, 256, N'Apple SSD Onboard', N'Apple M2 8-core GPU', 13.6, 60, 1.24, 52, N'macOS Sonoma'),
(98, N'Apple M2 8-core CPU', 16, 512, N'Apple SSD Onboard', N'Apple M2 10-core GPU', 13.6, 60, 1.24, 52, N'macOS Sonoma'),
(99, N'Apple M2 8-core CPU', 8, 256, N'Apple SSD Onboard', N'Apple M2 10-core GPU', 15.3, 60, 1.51, 66, N'macOS Sonoma'),
(100, N'Apple M3 8-core CPU', 8, 256, N'Apple SSD Onboard', N'Apple M3 8-core GPU', 13.6, 60, 1.24, 52, N'macOS Sonoma'),
(101, N'Apple M3 8-core CPU', 16, 512, N'Apple SSD Onboard', N'Apple M3 10-core GPU', 13.6, 60, 1.24, 52, N'macOS Sonoma'),
(102, N'Apple M3 8-core CPU', 16, 512, N'Apple SSD Onboard', N'Apple M3 10-core GPU', 15.3, 60, 1.51, 66, N'macOS Sonoma'),
(103, N'Apple M3 8-core CPU', 8, 512, N'Apple SSD Onboard', N'Apple M3 10-core GPU', 14.2, 120, 1.55, 70, N'macOS Sonoma'),
(104, N'Apple M3 8-core CPU', 16, 1024, N'Apple SSD Onboard', N'Apple M3 10-core GPU', 14.2, 120, 1.55, 70, N'macOS Sonoma'),
(105, N'Apple M3 Pro 11-core CPU', 18, 512, N'Apple SSD Onboard', N'Apple M3 Pro 14-core GPU', 14.2, 120, 1.61, 70, N'macOS Sonoma'),
(106, N'Apple M3 Pro 12-core CPU', 36, 1024, N'Apple SSD Onboard', N'Apple M3 Pro 18-core GPU', 14.2, 120, 1.61, 70, N'macOS Sonoma'),
(107, N'Apple M3 Max 14-core CPU', 36, 1024, N'Apple SSD Onboard', N'Apple M3 Max 30-core GPU', 14.2, 120, 1.62, 70, N'macOS Sonoma'),
(108, N'Apple M3 Pro 12-core CPU', 18, 512, N'Apple SSD Onboard', N'Apple M3 Pro 18-core GPU', 16.2, 120, 2.14, 100, N'macOS Sonoma'),
(109, N'Apple M3 Pro 12-core CPU', 36, 512, N'Apple SSD Onboard', N'Apple M3 Pro 18-core GPU', 16.2, 120, 2.14, 100, N'macOS Sonoma'),
(110, N'Apple M3 Max 16-core CPU', 48, 1024, N'Apple SSD Onboard', N'Apple M3 Max 40-core GPU', 16.2, 120, 2.16, 100, N'macOS Sonoma'),
(111, N'Intel Core i5-13420H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 2050 4GB', 15.6, 144, 2.1, 57, N'Windows 11 Home'),
(112, N'Intel Core i5-13420H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 15.6, 144, 2.1, 57, N'Windows 11 Home'),
(113, N'Intel Core i7-13620H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 15.6, 144, 2.1, 57, N'Windows 11 Home'),
(114, N'AMD Ryzen 7 8845HS', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 16.0, 165, 2.4, 57, N'Windows 11 Home'),
(115, N'Intel Core i5-12500H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3050 4GB', 15.6, 144, 2.5, 57, N'Windows 11 Home'),
(116, N'Intel Core i7-12700H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3050Ti 4GB', 15.6, 144, 2.5, 57, N'Windows 11 Home'),
(117, N'AMD Ryzen 7 7840HS', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.0, 165, 2.6, 90, N'Windows 11 Home'),
(118, N'Intel Core i7-13700H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 17.3, 165, 3.0, 90, N'Windows 11 Home'),
(119, N'Intel Core i7-13700HX', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.0, 165, 2.6, 90, N'Windows 11 Home'),
(120, N'Intel Core i7-14700HX', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 240, 2.6, 90, N'Windows 11 Home'),
(121, N'Intel Core i9-14900HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4080 12GB', 16.0, 250, 2.7, 90, N'Windows 11 Home'),
(122, N'Intel Core i9-14900HX', 64, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4090 16GB', 18.0, 250, 3.25, 90, N'Windows 11 Home'),
(123, N'Intel Core i3-1215U', 8, 512, N'SSD NVMe PCIe', N'Intel UHD Graphics', 15.6, 60, 1.7, 40, N'Windows 11 Home'),
(124, N'Intel Core i5-1235U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 15.6, 60, 1.7, 40, N'Windows 11 Home'),
(125, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 15.6, 60, 1.75, 50, N'Windows 11 Home'),
(126, N'Intel Core i5-12450H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 2050 4GB', 15.6, 144, 2.1, 50, N'Windows 11 Home'),
(127, N'Intel Core i5-13500H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 90, 1.25, 65, N'Windows 11 Home'),
(128, N'Intel Core Ultra 5 125H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 90, 1.3, 65, N'Windows 11 Home'),
(129, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 14.0, 60, 1.27, 58, N'Windows 11 Home'),
(130, N'Intel Core i7-13700H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 14.5, 120, 1.55, 76, N'Windows 11 Home'),
(131, N'AMD Ryzen 7 7840U', 16, 512, N'SSD NVMe PCIe 4.0', N'AMD Radeon 780M', 16.0, 120, 1.23, 54, N'Windows 11 Home'),
(132, N'Intel Core i5-1240P', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.25, 56, N'Windows 11 Home'),
(133, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 16.0, 120, 1.6, 65, N'Windows 11 Home'),
(134, N'AMD Ryzen 7 7840HS', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 16.0, 120, 1.9, 76, N'Windows 11 Home'),
(135, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 14.0, 60, 1.5, 50, N'Windows 11 Pro'),
(136, N'AMD Ryzen 5 7535HS', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 2050 4GB', 15.6, 144, 2.29, 52, N'Windows 11 Home'),
(137, N'Intel Core i5-12450H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3050 6GB', 15.6, 144, 2.29, 52, N'Windows 11 Home'),
(138, N'Intel Core i5-13420H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 15.6, 144, 2.29, 70, N'Windows 11 Home'),
(139, N'Intel Core i7-13700H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.1, 165, 2.31, 70, N'Windows 11 Home'),
(140, N'AMD Ryzen 7 7840HS', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.1, 165, 2.31, 70, N'Windows 11 Home'),
(141, N'Intel Core i7-13700H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.1, 165, 2.35, 83, N'Windows 11 Home'),
(142, N'Intel Core i9-13900HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4080 12GB', 16.1, 240, 2.39, 83, N'Windows 11 Home'),
(143, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 14.0, 120, 1.63, 71, N'Windows 11 Home'),
(144, N'AMD Ryzen 3 7320U', 8, 256, N'SSD NVMe PCIe', N'AMD Radeon 610M', 14.0, 60, 1.41, 41, N'Windows 11 Home'),
(145, N'Intel Core i3-1215U', 8, 512, N'SSD NVMe PCIe', N'Intel UHD Graphics', 15.6, 60, 1.69, 41, N'Windows 11 Home'),
(146, N'Intel Core i5-1235U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 15.6, 60, 1.69, 41, N'Windows 11 Home'),
(147, N'Intel Core i5-1235U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 14.0, 60, 1.41, 43, N'Windows 11 Home'),
(148, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 15.6, 60, 1.74, 41, N'Windows 11 Home'),
(149, N'Intel Core i7-1355U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 15.6, 60, 1.74, 41, N'Windows 11 Home'),
(150, N'Intel Core i5-13500H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 120, 1.44, 68, N'Windows 11 Home'),
(151, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.38, 51, N'Windows 11 Pro'),
(152, N'Intel Core i7-1355U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 15.6, 60, 1.79, 51, N'Windows 11 Pro'),
(153, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.52, 43, N'Windows 11 Home'),
(154, N'Intel Core Ultra 5 125H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 120, 1.39, 59, N'Windows 11 Home'),
(155, N'Intel Core i7-13700H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.0, 120, 2.3, 83, N'Windows 11 Home'),
(156, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 120, 1.44, 68, N'Windows 11 Home'),
(157, N'Intel Core i7-13700H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc A370M 4GB', 16.0, 60, 2.01, 83, N'Windows 11 Home'),
(158, N'Intel Core i7-1365U', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.36, 51, N'Windows 11 Pro'),
(159, N'Intel Core i7-1365U', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 120, 1.18, 51, N'Windows 11 Pro'),
(160, N'Intel Core i7-13700H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA RTX A1000 6GB', 15.6, 60, 2.0, 83, N'Windows 11 Pro'),
(161, N'Intel Core i5-12450H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 2050 4GB', 15.6, 144, 1.86, 52, N'Windows 11 Home'),
(162, N'Intel Core i5-13420H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3050 4GB', 15.6, 144, 1.86, 52, N'Windows 11 Home'),
(163, N'AMD Ryzen 5 7535HS', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3050 4GB', 15.6, 144, 1.86, 52, N'Windows 11 Home'),
(164, N'Intel Core i5-12450H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 15.6, 144, 1.98, 53, N'Windows 11 Home'),
(165, N'Intel Core i7-13620H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 15.6, 144, 1.98, 53, N'Windows 11 Home'),
(166, N'Intel Core i7-13620H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 15.6, 144, 2.25, 53, N'Windows 11 Home'),
(167, N'Intel Core i7-14700HX', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.0, 144, 2.3, 65, N'Windows 11 Home'),
(168, N'Intel Core i9-13980HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4080 12GB', 16.0, 240, 2.7, 90, N'Windows 11 Home'),
(169, N'Intel Core i9-14900HX', 32, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4080 12GB', 17.0, 240, 3.1, 99, N'Windows 11 Home'),
(170, N'Intel Core i9-14900HX', 128, 4096, N'SSD NVMe PCIe 5.0', N'NVIDIA GeForce RTX 4090 16GB', 18.0, 120, 3.6, 99, N'Windows 11 Pro'),
(171, N'Intel Core i7-13700H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 14.0, 165, 1.7, 72, N'Windows 11 Home'),
(172, N'Intel Core Ultra 9 185H', 32, 2048, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 240, 1.99, 99, N'Windows 11 Home'),
(173, N'Intel Core i3-1215U', 8, 512, N'SSD NVMe PCIe', N'Intel UHD Graphics', 14.0, 60, 1.4, 39, N'Windows 11 Home'),
(174, N'Intel Core i5-1335U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 14.0, 60, 1.4, 39, N'Windows 11 Home'),
(175, N'Intel Core i7-1355U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 15.6, 60, 1.7, 39, N'Windows 11 Home'),
(176, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 13.3, 60, 0.99, 75, N'Windows 11 Home'),
(177, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 16.0, 60, 1.5, 99, N'Windows 11 Home'),
(178, N'Intel Core i7-1360P', 16, 1024, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 60, 1.59, 72, N'Windows 11 Pro'),
(179, N'Intel Core i7-13700HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 17.0, 165, 2.49, 90, N'Windows 11 Pro'),
(180, N'Intel Core i7-13620H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 16.0, 60, 2.26, 53, N'Windows 11 Home'),
(181, N'Intel Core Ultra 5 125H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 14.0, 60, 1.12, 72, N'Windows 11 Home'),
(182, N'Intel Core Ultra 7 155H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 15.6, 60, 1.29, 72, N'Windows 11 Home'),
(183, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 16.0, 60, 1.19, 77, N'Windows 11 Home'),
(184, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 17.0, 60, 1.35, 80, N'Windows 11 Home'),
(185, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 3050 4GB', 16.0, 144, 1.28, 77, N'Windows 11 Home'),
(186, N'Intel Core Ultra 7 155H', 32, 1024, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 16.0, 120, 1.39, 77, N'Windows 11 Home'),
(187, N'Intel Core i7-1360P', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 14.0, 90, 0.99, 72, N'Windows 11 Home'),
(188, N'Intel Core i7-1360P', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Iris Xe Graphics', 15.6, 60, 0.99, 60, N'Windows 11 Home'),
(189, N'Intel Core i5-13500H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 15.6, 144, 2.08, 54, N'Windows 11 Home'),
(190, N'Intel Core i7-13620H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 15.6, 144, 2.08, 54, N'Windows 11 Home'),
(191, N'Intel Core i7-13620H', 16, 512, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 16.0, 165, 2.3, 54, N'Windows 11 Home'),
(192, N'Intel Core i7-13700H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 15.6, 165, 2.25, 99, N'Windows 11 Home'),
(193, N'Intel Core i7-14650HX', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4070 8GB', 16.0, 165, 2.3, 99, N'Windows 11 Home'),
(194, N'Intel Core i7-13700H', 16, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4050 6GB', 14.0, 90, 1.49, 63, N'Windows 11 Home'),
(195, N'Intel Core i5-1235U', 8, 256, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 13.5, 60, 1.27, 47, N'Windows 11 Home'),
(196, N'Intel Core i7-1255U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 13.5, 60, 1.29, 47, N'Windows 11 Home'),
(197, N'Intel Core i7-1255U', 16, 512, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 15.0, 60, 1.54, 47, N'Windows 11 Home'),
(198, N'Intel Core Ultra 5 135H', 16, 512, N'SSD NVMe PCIe 4.0', N'Intel Arc Graphics', 13.5, 60, 1.38, 47, N'Windows 11 Pro'),
(199, N'Intel Core i5-1235U', 8, 256, N'SSD NVMe PCIe', N'Intel Iris Xe Graphics', 13.0, 120, 0.88, 47, N'Windows 11 Home'),
(200, N'Intel Core i7-13700H', 32, 1024, N'SSD NVMe PCIe 4.0', N'NVIDIA GeForce RTX 4060 8GB', 14.4, 120, 1.98, 80, N'Windows 11 Home');
GO
