Add-Type -AssemblyName 'System.Data'

$conn = New-Object System.Data.SqlClient.SqlConnection('Server=.\SQLEXPRESS;Database=LaptopStoreDb;Integrated Security=True;TrustServerCertificate=True')
$conn.Open()

function Export-TableSql($tableName, $identityCol = $null) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT * FROM [$tableName]"
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    $adapter.Fill($dt) | Out-Null

    $sb = New-Object System.Text.StringBuilder
    $sb.AppendLine("-- ---------------------------------------------------------") | Out-Null
    $sb.AppendLine("-- DATA: $tableName ($($dt.Rows.Count) rows)") | Out-Null
    $sb.AppendLine("-- ---------------------------------------------------------") | Out-Null

    if ($dt.Rows.Count -eq 0) {
        return $sb.ToString()
    }

    $cols = @()
    foreach ($col in $dt.Columns) {
        $cols += "[$($col.ColumnName)]"
    }
    $colList = $cols -join ", "

    if ($identityCol) {
        $sb.AppendLine("SET IDENTITY_INSERT [$tableName] ON;") | Out-Null
    }

    $batchSize = 100
    $count = 0
    foreach ($row in $dt.Rows) {
        $vals = @()
        foreach ($col in $dt.Columns) {
            $val = $row[$col.ColumnName]
            if ($val -is [DBNull]) {
                $vals += "NULL"
            } elseif ($col.DataType -eq [bool]) {
                $vals += if ($val) { "1" } else { "0" }
            } elseif ($col.DataType -eq [int] -or $col.DataType -eq [long] -or $col.DataType -eq [decimal] -or $col.DataType -eq [double]) {
                $vals += $val.ToString().Replace(',', '.')
            } elseif ($col.DataType -eq [DateTime]) {
                $vals += "'$($val.ToString('yyyy-MM-dd HH:mm:ss.fff'))'"
            } else {
                $escaped = $val.ToString().Replace("'", "''")
                $vals += "N'$escaped'"
            }
        }
        $valList = $vals -join ", "
        $sb.AppendLine("INSERT INTO [$tableName] ($colList) VALUES ($valList);") | Out-Null
        $count++
        if ($count % $batchSize -eq 0) {
            $sb.AppendLine("GO") | Out-Null
        }
    }

    if ($identityCol) {
        $sb.AppendLine("SET IDENTITY_INSERT [$tableName] OFF;") | Out-Null
    }
    $sb.AppendLine("GO`n") | Out-Null
    return $sb.ToString()
}

$sbMain = New-Object System.Text.StringBuilder

$header = @"
-- =========================================================================
-- KHỞI TẠO TỰ ĐỘNG TOÀN DIỆN CSDL MICROSOFT SQL SERVER CHO LAPTOP STORE
-- Tự động kiểm tra: Nếu đã có CSDL LaptopStoreDb -> Xóa sạch để tạo mới
-- Tự động nạp: Cấu trúc 20 bảng + Dữ liệu đầy đủ (Sản phẩm, Biến thể, Đơn hàng, Trả góp, Bảo hành, Khách hàng)
-- Hỗ trợ Unicode Tiếng Việt chuẩn 100% (Collate: Vietnamese_CI_AS)
-- =========================================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'LaptopStoreDb')
BEGIN
    ALTER DATABASE [LaptopStoreDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [LaptopStoreDb];
END
GO

CREATE DATABASE [LaptopStoreDb] COLLATE Vietnamese_CI_AS;
GO

USE [LaptopStoreDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Bảng Thương hiệu (Brands)
CREATE TABLE Brands (
    BrandId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    BrandName NVARCHAR(255) NOT NULL,
    LogoUrl NVARCHAR(MAX) NULL,
    Description NVARCHAR(MAX) NULL
);
GO

-- 2. Bảng Danh mục (Categories)
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CategoryName NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL
);
GO

-- 3. Bảng Sản phẩm (Products)
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
    CONSTRAINT FK_Products_Categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES Categories (CategoryId) ON DELETE CASCADE,
    CONSTRAINT CK_Products_Price CHECK (Price >= 0),
    CONSTRAINT CK_Products_StockQuantity CHECK (StockQuantity >= 0)
);
CREATE NONCLUSTERED INDEX IX_Products_BrandId ON Products (BrandId);
CREATE NONCLUSTERED INDEX IX_Products_CategoryId ON Products (CategoryId);
GO

-- 4. Bảng Cấu hình chi tiết (ProductSpecifications - Quan hệ 1-1 với Products)
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

-- 5. Bảng Màu sắc (Colors)
CREATE TABLE Colors (
    ColorId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    HexCode NVARCHAR(20) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT IX_Colors_Name UNIQUE (Name)
);
GO

-- 6. Bảng Ảnh sản phẩm bổ sung (ProductImages)
CREATE TABLE ProductImages (
    ImageId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL,
    ColorId INT NULL,
    ImageUrl NVARCHAR(MAX) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_ProductImages_Products_ProductId FOREIGN KEY (ProductId) REFERENCES Products (ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_ProductImages_Colors_ColorId FOREIGN KEY (ColorId) REFERENCES Colors (ColorId) ON DELETE SET NULL
);
CREATE NONCLUSTERED INDEX IX_ProductImages_ProductId ON ProductImages (ProductId);
CREATE UNIQUE NONCLUSTERED INDEX IX_ProductImages_Product_Color_Slot ON ProductImages (ProductId, ColorId, DisplayOrder) WHERE ColorId IS NOT NULL;
GO

-- 7. Bảng Phiên bản cấu hình & Màu sắc (ProductVariants)
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
    CONSTRAINT FK_ProductVariants_Colors_ColorId FOREIGN KEY (ColorId) REFERENCES Colors (ColorId),
    CONSTRAINT CK_ProductVariants_Stock CHECK (StockQuantity >= 0)
);
GO

-- 8. Bảng Tài khoản người dùng (Users)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'Customer',
    GoogleId NVARCHAR(100) NULL,
    AvatarUrl NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT IX_Users_Username UNIQUE (Username),
    CONSTRAINT IX_Users_Email UNIQUE (Email)
);
GO

-- 9. Bảng Mã giảm giá & Khuyến mãi (Discounts)
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
GO

-- 10. Bảng Đơn đặt hàng (Orders)
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
    DiscountCode NVARCHAR(50) NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PurchaseMode NVARCHAR(50) NOT NULL DEFAULT 'Full',
    PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'COD',
    PaymentStatus NVARCHAR(50) NOT NULL DEFAULT N'Chờ thanh toán',
    OrderStatus NVARCHAR(50) NOT NULL DEFAULT N'Đang xử lý',
    IdempotencyKey NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,
    CONSTRAINT IX_Orders_OrderCode UNIQUE (OrderCode),
    CONSTRAINT FK_Orders_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (UserId) ON DELETE SET NULL
);
CREATE UNIQUE NONCLUSTERED INDEX IX_Orders_IdempotencyKey ON Orders (IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
GO

-- 11. Bảng Chi tiết mặt hàng trong đơn (OrderItems)
CREATE TABLE OrderItems (
    OrderItemId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    VariantId INT NULL,
    ColorName NVARCHAR(100) NULL,
    ProductName NVARCHAR(200) NOT NULL,
    SelectedSpecs NVARCHAR(255) NULL,
    ThumbnailUrl NVARCHAR(500) NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    TotalPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderItems_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES Orders (OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products_ProductId FOREIGN KEY (ProductId) REFERENCES Products (ProductId) ON DELETE NO ACTION,
    CONSTRAINT FK_OrderItems_ProductVariants_VariantId FOREIGN KEY (VariantId) REFERENCES ProductVariants (VariantId) ON DELETE SET NULL,
    CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0)
);
GO

-- 12. Bảng Hợp đồng trả góp (InstallmentContracts)
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
    CONSTRAINT FK_InstallmentContracts_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES Orders (OrderId) ON DELETE CASCADE,
    CONSTRAINT CK_InstallmentContracts_TermMonths CHECK (TermMonths IN (3, 6, 9, 12))
);
GO

-- 13. Bảng Lịch các kỳ trả góp (InstallmentSchedules)
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
    CONSTRAINT FK_InstallmentSchedules_Contracts_ContractId FOREIGN KEY (ContractId) REFERENCES InstallmentContracts (ContractId) ON DELETE CASCADE
);
GO

-- 14. Bảng Giao dịch thanh toán (Payments)
CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderId INT NOT NULL,
    PaymentType NVARCHAR(50) NOT NULL DEFAULT 'Full',
    ScheduleId INT NULL,
    ConfirmedBy NVARCHAR(100) NULL,
    PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'COD',
    Amount DECIMAL(18,2) NOT NULL,
    TransactionReference NVARCHAR(100) NULL,
    PaymentStatus NVARCHAR(50) NOT NULL DEFAULT N'Chờ thanh toán',
    PaymentDate DATETIME NOT NULL DEFAULT GETDATE(),
    Note NVARCHAR(255) NULL,
    CONSTRAINT FK_Payments_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES Orders (OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_Payments_InstallmentSchedules_ScheduleId FOREIGN KEY (ScheduleId) REFERENCES InstallmentSchedules (ScheduleId) ON DELETE NO ACTION
);
GO

ALTER TABLE InstallmentSchedules ADD CONSTRAINT FK_InstallmentSchedules_Payments_PaymentId 
    FOREIGN KEY (PaymentId) REFERENCES Payments (PaymentId) ON DELETE NO ACTION;
GO

-- 15. Bảng Nhật ký trạng thái đơn (OrderStatusHistories)
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

-- 16. Bảng Máy đã bàn giao theo số Serial (SoldDevices)
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
GO

-- 17. Bảng Danh mục dịch vụ Sửa chữa & Bảo hành (ServiceCatalog)
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
GO

-- 18. Bảng Phiếu tiếp nhận Dịch vụ & Bảo hành (ServiceTickets)
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
GO

-- 19. Bảng Báo giá dịch vụ (ServiceQuotes)
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
GO

-- 20. Bảng Nhật ký xử lý phiếu (ServiceTicketHistories)
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
GO

-- =========================================================================
-- NẠP DỮ LIỆU ĐẦY ĐỦ CHO TẤT CẢ CÁC BẢNG (DATA SEEDING)
-- =========================================================================
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

"@

$sbMain.Append($header) | Out-Null

$tablesToExport = @(
    @{ Name = "Brands"; Identity = "BrandId" },
    @{ Name = "Categories"; Identity = "CategoryId" },
    @{ Name = "Products"; Identity = "ProductId" },
    @{ Name = "ProductSpecifications"; Identity = $null },
    @{ Name = "Colors"; Identity = "ColorId" },
    @{ Name = "ProductImages"; Identity = "ImageId" },
    @{ Name = "ProductVariants"; Identity = "VariantId" },
    @{ Name = "Users"; Identity = "UserId" },
    @{ Name = "Discounts"; Identity = "DiscountId" },
    @{ Name = "Orders"; Identity = "OrderId" },
    @{ Name = "OrderItems"; Identity = "OrderItemId" },
    @{ Name = "InstallmentContracts"; Identity = "ContractId" },
    @{ Name = "InstallmentSchedules"; Identity = "ScheduleId" },
    @{ Name = "Payments"; Identity = "PaymentId" },
    @{ Name = "OrderStatusHistories"; Identity = "Id" },
    @{ Name = "SoldDevices"; Identity = "DeviceId" },
    @{ Name = "ServiceCatalog"; Identity = "ServiceId" },
    @{ Name = "ServiceTickets"; Identity = "TicketId" },
    @{ Name = "ServiceQuotes"; Identity = "QuoteId" },
    @{ Name = "ServiceTicketHistories"; Identity = "HistoryId" }
)

foreach ($t in $tablesToExport) {
    Write-Host "Exporting table $($t.Name)..."
    $tSql = Export-TableSql $t.Name $t.Identity
    $sbMain.Append($tSql) | Out-Null
}

$conn.Close()

$footer = @"
-- Bật lại kiểm tra toàn vẹn ràng buộc cho tất cả các bảng
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
GO

-- =========================================================================
-- KIỂM TRA TỔNG KẾT SAU KHI KHỞI TẠO CSDL
-- =========================================================================
SELECT t.NAME AS TableName, i.Rows AS TotalRows
FROM sys.tables t
JOIN sys.partitions i ON t.object_id = i.object_id
WHERE i.index_id IN (0, 1)
ORDER BY t.NAME;
GO

PRINT N'=========================================================================';
PRINT N'[THÀNH CÔNG] Cơ sở dữ liệu LaptopStoreDb đã được khởi tạo hoàn chỉnh!';
PRINT N'Đầy đủ: 200 Sản phẩm, 431 Biến thể, 67 Đơn hàng, Trả góp, Bảo hành & Người dùng.';
PRINT N'=========================================================================';
GO
"@

$sbMain.Append($footer) | Out-Null

[System.IO.File]::WriteAllText("$PSScriptRoot\sql-init\init-db.sql", $sbMain.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "SUCCESS: Generated sql-init\init-db.sql successfully!" -ForegroundColor Green
