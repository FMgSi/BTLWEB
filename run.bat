@echo off
chcp 65001 > nul

echo ========================================================
echo   KHỞI ĐỘNG HỆ THỐNG LAPTOP STORE (ASP.NET CORE MVC)
echo   Cơ sở dữ liệu: Microsoft SQL Server (Tự động nhận diện)
echo ========================================================

set LAUNCHED_BY_RUN_BAT=true

where npm >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    if exist "%~dp0frontend" (
        if not exist "%~dp0frontend\node_modules" (
            echo [*] Đang cài đặt thư viện Frontend React [npm install]...
            cd /d "%~dp0frontend"
            call npm install
            cd /d "%~dp0"
        )
        echo [*] Đang bật song song giao diện React Frontend (Port 3000)...
        start "LaptopStore-Frontend" /min cmd /c "cd /d "%~dp0frontend" && npm.cmd run dev"
    )
)

echo.
echo ========================================================
echo   👉 Cửa hàng Khách (Razor): http://localhost:5148/Catalog
echo   👉 Quản trị Sản phẩm     : http://localhost:5148/Product
echo   👉 Trang Quản trị Admin  : http://localhost:5148/Admin/Login
echo   👉 Tài khoản quản trị    : admin / Admin@123
echo ========================================================
echo.

dotnet run --project "%~dp0LaptopStore" --launch-profile http

