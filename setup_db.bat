@echo off
chcp 65001 > nul
title Laptop World - Thiet lap CSDL tu dong

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup_db.ps1"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================================
    echo   Co loi xay ra trong qua trinh thiet lap CSDL!
    echo ========================================================
    pause
)
