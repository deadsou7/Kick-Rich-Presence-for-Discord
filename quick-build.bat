@echo off
REM Simple build script for Windows users
REM Creates a standalone executable for Kick Stream Monitor

echo.
echo ========================================
echo   Kick Stream Monitor - Quick Build
echo ========================================
echo.

REM Check if .NET SDK is available
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found.
    echo Please install .NET 8.0 SDK or later from:
    echo https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Found .NET SDK
echo.

REM Clean previous builds
echo Cleaning previous builds...
if exist "publish" rmdir /s /q "publish"
if exist "dist" rmdir /s /q "dist"

echo.
echo Building standalone executable...
echo =================================

dotnet publish KickStatusChecker.Wpf\KickStatusChecker.Wpf.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output publish ^
    -p:PublishSingleFile=true ^
    -p:PublishReadyToRun=true ^
    -p:PublishTrimmed=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    --verbosity normal

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo.

REM Create distribution package
echo Creating distribution package...
mkdir dist 2>nul
copy publish\KickStatusChecker.Wpf.exe dist\
copy DISTRIBUTION_README.md dist\README.md

REM Show file size
for %%I in (dist\KickStatusChecker.Wpf.exe) do (
    echo File size: %%~zI bytes
)

echo.
echo ========================================
echo   Build Complete!
echo ========================================
echo.
echo Executable: dist\KickStatusChecker.Wpf.exe
echo Documentation: dist\README.md
echo.
echo To create a portable zip:
echo   cd dist
echo   powershell -Command "Compress-Archive -Path * -DestinationPath KickStreamMonitor-Portable.zip"
echo.
echo Press any key to exit...
pause >nul
