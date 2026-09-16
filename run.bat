@echo off
chcp 65001 > nul

echo ========================================================
echo   KHOI DONG HE THONG LAPTOP STORE (FULL STACK)
echo ========================================================
if not exist "%~dp0frontend\node_modules" (
    echo [!] Phat hien lan dau: Dang cai dat thu vien Frontend [npm install]
    cd /d "%~dp0frontend"
    call npm install
    cd /d "%~dp0"
    echo [*] Cai dat thu vien hoan tat!
    echo.
)

echo [1/2] Dang khoi dong React Frontend (Port 3000)...
start "LaptopStore-Frontend" /min cmd /c "cd /d "%~dp0frontend" && npm.cmd run dev"

echo [2/2] Dang khoi dong ASP.NET Core Backend (Port 5148)...
echo.
echo ========================================================
echo   * React Web Khach hang : http://localhost:3000
echo   * Trang Quan tri Admin : http://localhost:5148/Product
echo ========================================================
echo.

dotnet run --project "%~dp0LaptopStore" --launch-profile http
