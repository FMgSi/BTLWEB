@echo off
chcp 65001 > nul
echo ========================================================
echo   DANG DONG BO DU LIEU TU EXCEL VAO MYSQL DATABASE...
echo ========================================================
python sync_excel.py
pause
