Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "   KHỞI ĐỘNG HỆ THỐNG LAPTOP STORE (BACKEND + FRONTEND)   " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# Khởi động React Vite
Write-Host "[1/2] Đang khởi động React Frontend (Port 3000)..." -ForegroundColor Gray
Start-Process cmd -ArgumentList "/c npm.cmd run dev" -WorkingDirectory "$PSScriptRoot\frontend" -WindowStyle Minimized

# Khởi động C# Backend
Write-Host "[2/2] Đang khởi động ASP.NET Core Backend (Port 5148)..." -ForegroundColor Gray
Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  👉 React Web Khách hàng : http://localhost:3000" -ForegroundColor Green
Write-Host "  👉 Trang Quản trị Admin : http://localhost:5148/Product" -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

dotnet run --project "$PSScriptRoot\LaptopStore" --launch-profile http
