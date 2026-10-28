Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "   KHỞI ĐỘNG HỆ THỐNG LAPTOP STORE (ASP.NET CORE MVC)   " -ForegroundColor Cyan
Write-Host "   Cơ sở dữ liệu: Microsoft SQL Server (Tự động nhận diện)" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

$env:LAUNCHED_BY_RUN_BAT = "true"

$npmCmd = Get-Command npm -ErrorAction SilentlyContinue
if ($npmCmd -and (Test-Path "$PSScriptRoot\frontend")) {
    if (-not (Test-Path "$PSScriptRoot\frontend\node_modules")) {
        Write-Host "[*] Đang cài đặt thư viện Frontend React (npm install)..." -ForegroundColor Yellow
        Start-Process cmd -ArgumentList "/c npm install" -WorkingDirectory "$PSScriptRoot\frontend" -Wait -NoNewWindow
    }
    Write-Host "[*] Đang bật song song giao diện React Frontend (Port 3000)..." -ForegroundColor Gray
    Start-Process cmd -ArgumentList "/c npm.cmd run dev" -WorkingDirectory "$PSScriptRoot\frontend" -WindowStyle Minimized
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  👉 Cửa hàng Khách (Razor): http://localhost:5148/Catalog" -ForegroundColor Green
Write-Host "  👉 Quản trị Sản phẩm     : http://localhost:5148/Product" -ForegroundColor Green
Write-Host "  👉 Trang Quản trị Admin  : http://localhost:5148/Admin/Login" -ForegroundColor Yellow
Write-Host "  👉 Tài khoản quản trị    : admin / Admin@123" -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

dotnet run --project "$PSScriptRoot\LaptopStore" --launch-profile http
