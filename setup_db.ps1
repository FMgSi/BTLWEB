# =========================================================================
# SCRIPT TU DONG KIEM TRA, KHOI TAO VA KET NOI CO SO DU LIEU - LAPTOP STORE
# Tu dong nhan dien: SQLEXPRESS, LocalDB, localhost, .
# =========================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host "   LAPTOP WORLD - TU DONG THIET LAP CO SO DU LIEU CHO MAY MOI         " -ForegroundColor Cyan
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host ""

Add-Type -AssemblyName 'System.Data'

# 1. TU DONG PHAT HIEN INSTANCE SQL SERVER DANG HOAT DONG
Write-Host "[1/5] Dang kiem tra & phat hien SQL Server tren may nay..." -ForegroundColor Yellow

$candidateServers = @(
    ".\SQLEXPRESS",
    "(localdb)\MSSQLLocalDB",
    "localhost",
    "."
)

$detectedServer = $null
$masterConnStr = $null

foreach ($server in $candidateServers) {
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
        Write-Host "  -> Ket noi thanh cong toi SQL Server instance: $server" -ForegroundColor Green
        break
    } catch {
        # Thu server tiep theo
    }
}

if (-not $detectedServer) {
    Write-Host ""
    Write-Host "========================================================================" -ForegroundColor Red
    Write-Host "[LOI]: Khong the ket noi toi bat ky phien ban SQL Server nao tren may!" -ForegroundColor Red
    Write-Host "Vui long kiem tra lai:" -ForegroundColor Yellow
    Write-Host "1. Dich vu SQL Server (SQLEXPRESS hoac SQL Server) da duoc BAT (Running) chua." -ForegroundColor Yellow
    Write-Host "2. Hoac neu dung Visual Studio, dam bao 'SQL Server Express LocalDB' da duoc cai dat." -ForegroundColor Yellow
    Write-Host "========================================================================" -ForegroundColor Red
    Write-Host ""
    Read-Host "Nhan Enter de thoat..."
    exit 1
}

# 2. KIEM TRA SU TON TAI CUA CSDL VA XOA / TAO MOI SACH SE
Write-Host ""
Write-Host "[2/5] Dang kiem tra CSDL 'LaptopStoreDb'..." -ForegroundColor Yellow

$masterConn = New-Object System.Data.SqlClient.SqlConnection($masterConnStr)
$masterConn.Open()

$checkCmd = $masterConn.CreateCommand()
$checkCmd.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = 'LaptopStoreDb'"
$exists = [int]$checkCmd.ExecuteScalar()

if ($exists -gt 0) {
    Write-Host "  -> Phat hien CSDL 'LaptopStoreDb' da ton tai tren may." -ForegroundColor Gray
    Write-Host "  -> Dang ngat ket noi va xoa sach CSDL cu theo yeu cau..." -ForegroundColor Yellow
    $dropCmd = $masterConn.CreateCommand()
    $dropCmd.CommandText = "ALTER DATABASE [LaptopStoreDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [LaptopStoreDb];"
    $dropCmd.ExecuteNonQuery() | Out-Null
    Write-Host "  -> [OK] Da xoa hoan toan CSDL cu thanh cong." -ForegroundColor Green
} else {
    Write-Host "  -> CSDL 'LaptopStoreDb' chua ton tai. Se tien hanh tao moi." -ForegroundColor Gray
}

# 3. NAP TOAN BO CAU TRUC 20 BANG VA DU LIEU MAU TU SQL-INIT\INIT-DB.SQL
Write-Host ""
Write-Host "[3/5] Dang khoi tao CSDL moi va nap toan bo du lieu mau..." -ForegroundColor Yellow

$sqlFile = "$PSScriptRoot\sql-init\init-db.sql"
if (-not (Test-Path $sqlFile)) {
    Write-Host "[LOI]: Khong tim thay file $sqlFile!" -ForegroundColor Red
    $masterConn.Close()
    Read-Host "Nhan Enter de thoat..."
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
        Write-Host "  [Canh bao khoi $batchIndex]: $($_.Exception.Message)" -ForegroundColor Red
        $errorCount++
    }
}

if ($errorCount -eq 0) {
    Write-Host "  -> [OK] Da thuc thi thanh cong toan bo $totalBatches khoi lenh SQL!" -ForegroundColor Green
} else {
    Write-Host "  -> Da thuc thi voi $errorCount canh bao." -ForegroundColor Yellow
}

# Kiem tra tong ket so luong ban ghi trong cac bang
Write-Host ""
Write-Host "--- TONG KET DU LIEU CSDL LAPTOPSTOREDB VUA TAO ---" -ForegroundColor Cyan
$summaryCmd = $masterConn.CreateCommand()
$summaryCmd.CommandText = "SELECT t.NAME AS TableName, i.Rows FROM LaptopStoreDb.sys.tables t JOIN LaptopStoreDb.sys.partitions i ON t.object_id = i.object_id WHERE i.index_id IN (0, 1) ORDER BY t.NAME"
$reader = $summaryCmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host ("  + " + $reader[0].ToString().PadRight(25) + " : " + $reader[1].ToString() + " ban ghi") -ForegroundColor White
}
$reader.Close()
$masterConn.Close()

# 4. DONG BO CAU HINH APPSETTINGS.JSON CHO PHU HOP VOI SQL SERVER TREN MAY NAY
Write-Host ""
Write-Host "[4/5] Dong bo chuoi ket noi trong appsettings.json..." -ForegroundColor Yellow

$appsettingsPath = "$PSScriptRoot\LaptopStore\appsettings.json"
if (Test-Path $appsettingsPath) {
    try {
        $jsonContent = Get-Content $appsettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $newConnStr = "Server=$detectedServer;Database=LaptopStoreDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
        $jsonContent.ConnectionStrings.DefaultConnection = $newConnStr
        $updatedJson = $jsonContent | ConvertTo-Json -Depth 10
        [System.IO.File]::WriteAllText($appsettingsPath, $updatedJson, [System.Text.Encoding]::UTF8)
        Write-Host "  -> [OK] Da tro DefaultConnection toi: $detectedServer" -ForegroundColor Green
    } catch {
        Write-Host "  [Canh bao]: Khong the tu dong ghi appsettings.json: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# 5. KIEM TRA FRONTEND REACT VA KHOI CHAY CHUONG TRINH
Write-Host ""
Write-Host "[5/5] Chuan bi khoi chay chuong trinh..." -ForegroundColor Yellow

$env:LAUNCHED_BY_RUN_BAT = "true"

$npmCmd = Get-Command npm -ErrorAction SilentlyContinue
if ($npmCmd -and (Test-Path "$PSScriptRoot\frontend")) {
    if (-not (Test-Path "$PSScriptRoot\frontend\node_modules")) {
        Write-Host "  -> Phat hien lan dau tren may moi: Dang cai dat thu vien Frontend React [npm install]..." -ForegroundColor Yellow
        Start-Process cmd -ArgumentList "/c npm install" -WorkingDirectory "$PSScriptRoot\frontend" -Wait -NoNewWindow
    }
    Write-Host "  -> Dang bat may chu Frontend React (Port 3000)..." -ForegroundColor Green
    Start-Process cmd -ArgumentList "/c npm.cmd run dev" -WorkingDirectory "$PSScriptRoot\frontend" -WindowStyle Minimized
}

Write-Host ""
Write-Host "========================================================================" -ForegroundColor Green
Write-Host "   THIET LAP HOAN TAT! DANG KET NOI VA MO CHUONG TRINH                 " -ForegroundColor Green
Write-Host "========================================================================" -ForegroundColor Green
Write-Host "  -> Cua hang Khach hang  : http://localhost:3000 (hoac http://localhost:5148/Catalog)" -ForegroundColor White
Write-Host "  -> Quan tri Admin       : http://localhost:5148/Admin/Login" -ForegroundColor White
Write-Host "  -> Tai khoan Admin      : admin / Admin@123" -ForegroundColor Yellow
Write-Host "  -> Tai khoan Khach hang : 0354120145 (hoac batrunghoang832@gmail.com / User@123)" -ForegroundColor Yellow
Write-Host "========================================================================" -ForegroundColor Green
Write-Host ""

Start-Process powershell -ArgumentList "-NoProfile", "-Command", "Start-Sleep -Seconds 3; Start-Process 'http://localhost:5148/Admin/Login'; Start-Process 'http://localhost:3000'" -WindowStyle Hidden

dotnet run --project "$PSScriptRoot\LaptopStore" --launch-profile http
