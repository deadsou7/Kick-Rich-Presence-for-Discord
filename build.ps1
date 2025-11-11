# Build script for Kick Stream Monitor (Windows PowerShell)
# Creates a standalone Windows executable with all dependencies bundled

Write-Host "Building Kick Stream Monitor..." -ForegroundColor Green

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj --configuration Release --verbosity minimal

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj --verbosity minimal

# Build and publish as single file
Write-Host "Building standalone executable..." -ForegroundColor Yellow
dotnet publish KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output ./publish `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:PublishTrimmed=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
    Write-Host "📦 Executable location: ./publish/KickStatusChecker.Wpf.exe" -ForegroundColor Cyan
    
    # Show file size
    $exePath = "./publish/KickStatusChecker.Wpf.exe"
    if (Test-Path $exePath) {
        $size = (Get-Item $exePath).Length
        $sizeMB = [math]::Round($size / 1MB, 2)
        Write-Host "📊 File size: ${sizeMB}MB" -ForegroundColor Cyan
    }
    
    # Create distribution package
    Write-Host "📦 Creating distribution package..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path "./dist" | Out-Null
    Copy-Item "./publish/KickStatusChecker.Wpf.exe" "./dist/" -Force
    Copy-Item "README.md" "./dist/" -Force
    
    Write-Host "✅ Distribution package created in ./dist/" -ForegroundColor Green
    Write-Host ""
    Write-Host "To create a portable zip:" -ForegroundColor Cyan
    Write-Host "cd dist; Compress-Archive -Path * -DestinationPath KickStreamMonitor-v1.0.0.zip" -ForegroundColor Gray
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
