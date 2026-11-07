# =========================================================================
# SCRIPT TỰ ĐỘNG KIỂM TRA, KHỞI TẠO VÀ KẾT NỐI CƠ SỞ DỮ LIỆU - LAPTOP STORE
# Hỗ trợ tự động chạy trên mọi máy tính mới (SQLEXPRESS, LocalDB, localhost, .)
# =========================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host "   LAPTOP WORLD - TỰ ĐỘNG THIẾT LẬP CƠ SỞ DỮ LIỆU CHO MÁY MỚI         " -ForegroundColor Cyan
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host ""

Add-Type -AssemblyName 'System.Data'

# 1. TỰ ĐỘNG PHÁT HIỆN INSTANCE SQL SERVER ĐANG HOẠT ĐỘNG
Write-Host "[1/5] Đang kiểm tra & phát hiện SQL Server trên máy này..." -ForegroundColor Yellow

$candidateServers = @(
    ".\SQLEXPRESS",
    "(localdb)\MSSQLLocalDB",
    "localhost",
    "."
)

$detectedServer = $null
$masterConnStr = $null

foreach ($server in $candidateServers) {
    # Nếu là localdb, thử bật MSSQLLocalDB trước nếu chưa chạy
    if ($server -like "*(localdb)*") {
        $sqllocaldbCmd = Get-Command "sqllocaldb" -ErrorAction SilentlyContinue
        if ($sqllocaldbCmd) {
            try {
                Start-Process "sqllocaldb" -ArgumentList "start MSSQLLocalDB" -Wait -NoNewWindow -ErrorAction SilentlyContinue
            } catch {}
        }
    }

    $testStr = "Server=$server;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=3"
    try {
        $testConn = New-Object System.Data.SqlClient.SqlConnection($testStr)
        $testConn.Open()
        $testConn.Close()
        $detectedServer = $server
        $masterConnStr = $testStr
        Write-Host "  -> Kết nối thành công tới SQL Server instance: $server" -ForegroundColor Green
        break
    } catch {
        # Bỏ qua và thử server tiếp theo
    }
}

if (-not $detectedServer) {
    Write-Host ""
    Write-Host "========================================================================" -ForegroundColor Red
    Write-Host "[LỖI]: Không thể kết nối tới bất kỳ phiên bản SQL Server nào trên máy!" -ForegroundColor Red
    Write-Host "Vui lòng kiểm tra lại:" -ForegroundColor Yellow
    Write-Host "1. Dịch vụ SQL Server (SQLEXPRESS hoặc SQL Server) đã được BẬT (Running) chưa." -ForegroundColor Yellow
    Write-Host "2. Hoặc nếu dùng Visual Studio, đảm bảo tính năng 'SQL Server Express LocalDB' đã được cài đặt." -ForegroundColor Yellow
    Write-Host "========================================================================" -ForegroundColor Red
    Write-Host ""
    Read-Host "Nhấn Enter để thoát..."
    exit 1
}

# 2. KIỂM TRA SỰ TỒN TẠI CỦA CSDL VÀ XÓA / TẠO MỚI SẠCH SẼ
Write-Host ""
Write-Host "[2/5] Đang kiểm tra CSDL 'LaptopStoreDb'..." -ForegroundColor Yellow

$masterConn = New-Object System.Data.SqlClient.SqlConnection($masterConnStr)
$masterConn.Open()

$checkCmd = $masterConn.CreateCommand()
$checkCmd.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = 'LaptopStoreDb'"
$exists = [int]$checkCmd.ExecuteScalar()

if ($exists -gt 0) {
    Write-Host "  -> Phát hiện CSDL 'LaptopStoreDb' đã tồn tại trên máy." -ForegroundColor Gray
    Write-Host "  -> Đang ngắt kết nối và xóa sạch CSDL cũ theo yêu cầu..." -ForegroundColor Yellow
    $dropCmd = $masterConn.CreateCommand()
    $dropCmd.CommandText = "ALTER DATABASE [LaptopStoreDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [LaptopStoreDb];"
    $dropCmd.ExecuteNonQuery() | Out-Null
    Write-Host "  -> [OK] Đã xóa hoàn toàn CSDL cũ thành công." -ForegroundColor Green
} else {
    Write-Host "  -> CSDL 'LaptopStoreDb' chưa tồn tại. Sẽ tiến hành tạo mới." -ForegroundColor Gray
}

# 3. NẠP TOÀN BỘ CẤU TRÚC 20 BẢNG VÀ DỮ LIỆU MẪU TỪ SQL-INIT\INIT-DB.SQL
Write-Host ""
Write-Host "[3/5] Đang khởi tạo CSDL mới và nạp toàn bộ dữ liệu mẫu..." -ForegroundColor Yellow

$sqlFile = "$PSScriptRoot\sql-init\init-db.sql"
if (-not (Test-Path $sqlFile)) {
    Write-Host "[LỖI]: Không tìm thấy file $sqlFile!" -ForegroundColor Red
    $masterConn.Close()
    Read-Host "Nhấn Enter để thoát..."
    exit 1
}

$rawSql = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
$batches = [System.Text.RegularExpressions.Regex]::Split($rawSql, '^\s*GO\s*$', [System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

$batchIndex = 0
$totalBatches = $batches.Count
$errorCount = 0

foreach ($batch in $batches) {
    $batchIndex++
    $cleanBatch = $batch.Trim()
    if ([string]::IsNullOrWhiteSpace($cleanBatch)) { continue }

    try {
        $execCmd = $masterConn.CreateCommand()
        $execCmd.CommandText = $cleanBatch
        $execCmd.CommandTimeout = 120
        $execCmd.ExecuteNonQuery() | Out-Null
    } catch {
        Write-Host "  [Cảnh báo khối $batchIndex]: $($_.Exception.Message)" -ForegroundColor Red
        $errorCount++
    }
}

if ($errorCount -eq 0) {
    Write-Host "  -> [OK] Đã thực thi thành công toàn bộ $totalBatches khối lệnh SQL!" -ForegroundColor Green
} else {
    Write-Host "  -> Đã thực thi với $errorCount cảnh báo." -ForegroundColor Yellow
}

# Kiểm tra tổng kết số lượng bản ghi trong các bảng
Write-Host ""
Write-Host "--- TỔNG KẾT DỮ LIỆU CSDL LAPTOPSTOREDB VỪA TẠO ---" -ForegroundColor Cyan
$summaryCmd = $masterConn.CreateCommand()
$summaryCmd.CommandText = @"
SELECT t.NAME AS TableName, i.Rows 
FROM LaptopStoreDb.sys.tables t 
JOIN LaptopStoreDb.sys.partitions i ON t.object_id = i.object_id 
WHERE i.index_id IN (0, 1) 
ORDER BY t.NAME
"@
$reader = $summaryCmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host ("  + " + $reader[0].ToString().PadRight(25) + " : " + $reader[1].ToString() + " bản ghi") -ForegroundColor White
}
$reader.Close()
$masterConn.Close()

# 4. ĐỒNG BỘ CẤU HÌNH APPSETTINGS.JSON CHO PHÙ HỢP VỚI SQL SERVER TRÊN MÁY NÀY
Write-Host ""
Write-Host "[4/5] Đồng bộ chuỗi kết nối trong appsettings.json..." -ForegroundColor Yellow

$appsettingsPath = "$PSScriptRoot\LaptopStore\appsettings.json"
if (Test-Path $appsettingsPath) {
    try {
        $jsonContent = Get-Content $appsettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $newConnStr = "Server=$detectedServer;Database=LaptopStoreDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
        $jsonContent.ConnectionStrings.DefaultConnection = $newConnStr
        $updatedJson = $jsonContent | ConvertTo-Json -Depth 10
        [System.IO.File]::WriteAllText($appsettingsPath, $updatedJson, [System.Text.Encoding]::UTF8)
        Write-Host "  -> [OK] Đã trỏ DefaultConnection tới: $detectedServer" -ForegroundColor Green
    } catch {
        Write-Host "  [Cảnh báo]: Không thể tự động ghi appsettings.json: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# 5. KIỂM TRA FRONTEND REACT VÀ KHỞI CHẠY CHƯƠNG TRÌNH
Write-Host ""
Write-Host "[5/5] Chuẩn bị khởi chạy chương trình..." -ForegroundColor Yellow

$env:LAUNCHED_BY_RUN_BAT = "true"

$npmCmd = Get-Command npm -ErrorAction SilentlyContinue
if ($npmCmd -and (Test-Path "$PSScriptRoot\frontend")) {
    if (-not (Test-Path "$PSScriptRoot\frontend\node_modules")) {
        Write-Host "  -> Phát hiện lần đầu trên máy mới: Đang cài đặt thư viện Frontend React [npm install]..." -ForegroundColor Yellow
        Start-Process cmd -ArgumentList "/c npm install" -WorkingDirectory "$PSScriptRoot\frontend" -Wait -NoNewWindow
    }
    Write-Host "  -> Đang bật máy chủ Frontend React (Port 3000)..." -ForegroundColor Green
    Start-Process cmd -ArgumentList "/c npm.cmd run dev" -WorkingDirectory "$PSScriptRoot\frontend" -WindowStyle Minimized
}

Write-Host ""
Write-Host "========================================================================" -ForegroundColor Green
Write-Host "   THIẾT LẬP HOÀN TẤT! ĐANG KẾT NỐI VÀ MỞ CHƯƠNG TRÌNH                 " -ForegroundColor Green
Write-Host "========================================================================" -ForegroundColor Green
Write-Host "  👉 Cửa hàng Khách hàng  : http://localhost:3000 (hoặc http://localhost:5148/Catalog)" -ForegroundColor White
Write-Host "  👉 Quản trị Admin       : http://localhost:5148/Admin/Login" -ForegroundColor White
Write-Host "  👉 Tài khoản Admin      : admin / Admin@123" -ForegroundColor Yellow
Write-Host "  👉 Tài khoản Khách hàng : 0354120145 (hoặc batrunghoang832@gmail.com / User@123)" -ForegroundColor Yellow
Write-Host "========================================================================" -ForegroundColor Green
Write-Host ""

# Tự động mở trình duyệt sau 3 giây khi backend khởi động
Start-Process powershell -ArgumentList "-NoProfile -Command `"Start-Sleep -Seconds 3; Start-Process 'http://localhost:5148/Admin/Login'; Start-Process 'http://localhost:3000'`"" -WindowStyle Hidden

dotnet run --project "$PSScriptRoot\LaptopStore" --launch-profile http

