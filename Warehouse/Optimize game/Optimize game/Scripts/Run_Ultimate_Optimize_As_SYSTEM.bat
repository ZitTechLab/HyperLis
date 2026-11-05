@echo off
:: Kiem tra quyen Admin
net session >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] Dang chay duoi quyen Administrator.
) else (
    echo [Loi] Vui long nhan chuot phai vao file nay va chon "Run as Administrator"!
    pause
    exit
)

echo.
echo Dang khoi dong Ultimate_Optimize.ps1 voi quyen SYSTEM toi cao (vuot qua TrustedInstaller)...
echo cua so moi se hien ra va TU DONG LUU LOG vao file: SYSTEM_Optimize_Log.txt
echo Vui long cho trong giay lat...
echo.

:: Khoi chay PsExec, goi PowerShell voi tham so -NoExit de khong tu dong dong cua so.
:: Va dung Start-Transcript de ghi log ngay tu dau ra file SYSTEM_Optimize_Log.txt
"%~dp0PSTools\PsExec.exe" -accepteula -i -s powershell.exe -NoExit -ExecutionPolicy Bypass -Command "Start-Transcript -Path '%~dp0SYSTEM_Optimize_Log.txt'; & '%~dp0Ultimate_Optimize.ps1'; Stop-Transcript; Write-Host 'DA CHAY XONG! Ban co the tat cua so nay va kiem tra file SYSTEM_Optimize_Log.txt' -ForegroundColor Green"

echo.
echo Hoan tat lenh goi! Kiem tra cua so mau xanh SYSTEM.
pause
