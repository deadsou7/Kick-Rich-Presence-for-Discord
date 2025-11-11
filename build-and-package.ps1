# Comprehensive build and packaging script for Kick Stream Monitor (Windows PowerShell)
# Creates both portable executable and Windows installer

param(
    [switch]$SkipInstaller,
    [switch]$OnlyPortable,
    [string]$Version = "1.0.0"
)

# Configuration
$AppName = "KickStreamMonitor"
$OutputDir = ".\dist"
$PublishDir = ".\publish"
$InstallerDir = ".\installer"

Write-Host "🚀 Kick Stream Monitor - Build & Package Script" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

# Clean previous builds
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
Remove-Item -Path $OutputDir, $PublishDir, $InstallerDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $OutputDir, $InstallerDir | Out-Null

# Function to build standalone executable
function Build-Executable {
    Write-Host ""
    Write-Host "📦 Building standalone executable..." -ForegroundColor Cyan
    Write-Host "-----------------------------------" -ForegroundColor Cyan
    
    try {
        # Check if dotnet is available
        $dotnetVersion = dotnet --version 2>$null
        if (-not $dotnetVersion) {
            Write-Host "❌ .NET SDK not found. Please install .NET 8.0 SDK or later." -ForegroundColor Red
            return $false
        }
        Write-Host "✅ Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
        
        # Clean and restore
        Write-Host "🔧 Cleaning and restoring dependencies..." -ForegroundColor Yellow
        dotnet clean KickStatusChecker.Wpf\KickStatusChecker.Wpf.csproj --configuration Release --verbosity minimal
        dotnet restore KickStatusChecker.Wpf\KickStatusChecker.Wpf.csproj --verbosity minimal
        
        # Build and publish
        Write-Host "🏗️  Building executable..." -ForegroundColor Yellow
        dotnet publish KickStatusChecker.Wpf\KickStatusChecker.Wpf.csproj `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --output $PublishDir `
            -p:PublishSingleFile=true `
            -p:PublishReadyToRun=true `
            -p:PublishTrimmed=false `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:EnableCompressionInSingleFile=true `
            --verbosity normal
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Executable build completed successfully!" -ForegroundColor Green
            
            # Show file size
            $exePath = "$PublishDir\KickStatusChecker.Wpf.exe"
            if (Test-Path $exePath) {
                $size = (Get-Item $exePath).Length
                $sizeMB = [math]::Round($size / 1MB, 2)
                Write-Host "📊 File size: ${sizeMB}MB" -ForegroundColor Cyan
            }
            return $true
        } else {
            Write-Host "❌ Executable build failed!" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "❌ Build error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to create portable package
function New-PortablePackage {
    Write-Host ""
    Write-Host "📁 Creating portable package..." -ForegroundColor Cyan
    Write-Host "-------------------------------" -ForegroundColor Cyan
    
    $PortableDir = "$OutputDir\${AppName}-Portable-v${Version}"
    New-Item -ItemType Directory -Force -Path $PortableDir | Out-Null
    
    # Copy executable
    Copy-Item "$PublishDir\KickStatusChecker.Wpf.exe" "$PortableDir\" -Force
    
    # Copy documentation
    Copy-Item "DISTRIBUTION_README.md" "$PortableDir\README.md" -Force
    
    # Create version info file
    $versionInfo = @"
Kick Stream Monitor v${Version}
Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Platform: Windows x64
Type: Portable (No installation required)
"@
    $versionInfo | Out-File -FilePath "$PortableDir\VERSION.txt" -Encoding UTF8
    
    # Create portable zip
    $zipPath = "$OutputDir\${AppName}-Portable-v${Version}.zip"
    Compress-Archive -Path "$PortableDir\*" -DestinationPath $zipPath -Force
    
    Write-Host "✅ Portable package created: $zipPath" -ForegroundColor Green
}

# Function to create installer
function New-Installer {
    if ($SkipInstaller) {
        Write-Host ""
        Write-Host "⏭️  Skipping installer creation (SkipInstaller specified)" -ForegroundColor Yellow
        return
    }
    
    Write-Host ""
    Write-Host "🔧 Creating Windows installer..." -ForegroundColor Cyan
    Write-Host "--------------------------------" -ForegroundColor Cyan
    
    try {
        # Check if WiX tools are available
        $wixTool = dotnet tool list --global | Select-String "wix"
        if (-not $wixTool) {
            Write-Host "📦 Installing WiX Toolset..." -ForegroundColor Yellow
            dotnet tool install --global wix --version 4.0.0
            
            # Add to PATH for current session
            $env:PATH = "$env:PATH;$env:USERPROFILE\.dotnet\tools"
        }
        
        # Build installer project
        Write-Host "🏗️  Building installer..." -ForegroundColor Yellow
        Set-Location "KickStatusChecker.Wpf"
        
        dotnet build KickStatusChecker.Wpf.Setup.wixproj --configuration Release --output "..\$InstallerDir"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Installer created successfully!" -ForegroundColor Green
            
            # Copy installer to output directory
            $msiFiles = Get-ChildItem -Path "..\$InstallerDir" -Filter "*.msi"
            foreach ($msi in $msiFiles) {
                $destPath = "$OutputDir\${AppName}-Setup-v${Version}.msi"
                Copy-Item $msi.FullName $destPath -Force
                Write-Host "📦 Installer location: $destPath" -ForegroundColor Cyan
            }
        } else {
            Write-Host "❌ Installer build failed!" -ForegroundColor Red
            Write-Host "💡 You can still use the portable version" -ForegroundColor Yellow
        }
        
        Set-Location ".."
    } catch {
        Write-Host "❌ Installer creation error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "💡 You can still use the portable version" -ForegroundColor Yellow
    }
}

# Function to create checksums
function New-Checksums {
    Write-Host ""
    Write-Host "🔐 Creating checksums..." -ForegroundColor Cyan
    Write-Host "------------------------" -ForegroundColor Cyan
    
    Set-Location $OutputDir
    
    $checksumFile = "checksums.txt"
    $files = Get-ChildItem -Filter "*.zip", "*.msi"
    
    if ($files.Count -gt 0) {
        $checksums = @()
        foreach ($file in $files) {
            $hash = Get-FileHash -Path $file.FullName -Algorithm SHA256
            $checksums += "$($hash.Hash.ToLower())  $($file.Name)"
        }
        
        $checksums | Out-File -FilePath $checksumFile -Encoding UTF8
        Write-Host "✅ Checksums created: $OutputDir\$checksumFile" -ForegroundColor Green
        
        # Display checksums
        Write-Host ""
        Write-Host "📋 File checksums:" -ForegroundColor Cyan
        Get-Content $checksumFile | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
    }
    
    Set-Location ".."
}

# Function to display summary
function Show-Summary {
    Write-Host ""
    Write-Host "🎉 Build completed successfully!" -ForegroundColor Green
    Write-Host "=================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "📦 Output files created in: $OutputDir" -ForegroundColor Cyan
    Write-Host ""
    
    # List created files
    if (Test-Path $OutputDir) {
        Write-Host "📋 Created files:" -ForegroundColor Cyan
        Get-ChildItem $OutputDir | ForEach-Object {
            $size = [math]::Round($_.Length / 1MB, 2)
            Write-Host "   $($_.Name) ($($size)MB)" -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "📖 Installation Instructions:" -ForegroundColor Cyan
    Write-Host "------------------------------" -ForegroundColor Gray
    Write-Host "📁 Portable Version:" -ForegroundColor White
    Write-Host "   1. Download ${AppName}-Portable-v${Version}.zip" -ForegroundColor Gray
    Write-Host "   2. Extract to any folder" -ForegroundColor Gray
    Write-Host "   3. Run KickStatusChecker.Wpf.exe" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔧 Installer Version:" -ForegroundColor White
    Write-Host "   1. Download ${AppName}-Setup-v${Version}.msi" -ForegroundColor Gray
    Write-Host "   2. Run the installer" -ForegroundColor Gray
    Write-Host "   3. Follow the installation wizard" -ForegroundColor Gray
    Write-Host ""
    Write-Host "📖 For detailed instructions, see README.md in the package" -ForegroundColor Gray
}

# Main execution
try {
    Write-Host "Starting build process..." -ForegroundColor Green
    
    # Build executable
    if (Build-Executable) {
        # Create portable package
        New-PortablePackage
        
        # Create installer (unless skipped)
        if (-not $OnlyPortable) {
            New-Installer
        } else {
            Write-Host ""
            Write-Host "⏭️  Skipping installer (OnlyPortable specified)" -ForegroundColor Yellow
        }
        
        # Create checksums
        New-Checksums
        
        # Display summary
        Show-Summary
        
        Write-Host ""
        Write-Host "✅ All done! Your Kick Stream Monitor is ready for distribution." -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "❌ Build process failed. Please check the error messages above." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "❌ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
