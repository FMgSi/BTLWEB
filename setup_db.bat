@echo off
chcp 65001 > nul
title Laptop World - Setup & Reset Database

echo ========================================================================
echo   LAPTOP WORLD - TỰ ĐỘNG THIẾT LẬP CƠ SỞ DỮ LIỆU CHO MÁY MỚI
echo ========================================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup_db.ps1"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================================================
    echo   Đã xảy ra lỗi trong quá trình khởi tạo CSDL!
    echo ========================================================================
    pause
)
