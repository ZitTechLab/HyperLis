@echo off
:: Kiểm tra quyền Admin
TITLE Dang cai dat Driver...
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"

if %errorlevel% NEQ 0 (
    echo Dang yeu cau quyen Administrator...
    powershell start-process -FilePath '%0' -verb runas
    exit /b
)

:: Di chuyen den thu muc chua file .bat
cd /d "%~dp0"

echo Dang quet va cai dat tat ca driver (.inf)...
echo Thu muc hien tai: %cd%
echo ---------------------------------------------------

:: Chay cau lenh cai dat driver
pnputil /add-driver *.inf /subdirs /install

echo ---------------------------------------------------
echo Hoan tat! Nhan phim bat ky de thoat.
pause