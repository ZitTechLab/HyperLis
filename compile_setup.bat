@echo off
setlocal

echo ==========================================
echo        Building HyperLis Project
echo ==========================================

REM Always run from the script directory to avoid MSB1003
pushd "%~dp0"
set "PROJECT_FILE=%~dp0HyperLisUI.csproj"

if not exist "%PROJECT_FILE%" (
    echo [ERROR] Project file not found: "%PROJECT_FILE%"
    pause
    exit /b 1
)

echo [1/3] Cleaning old build files...
dotnet restore "%PROJECT_FILE%"
dotnet clean "%PROJECT_FILE%" -c Release
if %errorlevel% neq 0 (
    echo [ERROR] Dotnet clean failed!
    popd
    pause
    exit /b %errorlevel%
)
if exist "bin\Release" rmdir /s /q "bin\Release"

echo.
echo [2/3] Publishing application for win-x64...
dotnet publish "%PROJECT_FILE%" -c Release -r win-x64
if %errorlevel% neq 0 (
    echo [ERROR] Dotnet publish failed!
    popd
    pause
    exit /b %errorlevel%
)

echo.
echo [3/3] Compiling Inno Setup script...
if not exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    echo [ERROR] Inno Setup compiler ^(ISCC.exe^) not found! Please check the installation path.
    popd
    pause
    exit /b 1
)
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "%~dp0HyperLis.iss"
if %errorlevel% neq 0 (
    echo [ERROR] Inno Setup compilation failed!
    popd
    pause
    exit /b %errorlevel%
)

echo.
echo ==========================================
echo    SUCCESS: Setup file generated in Output\
echo ==========================================
popd
pause
