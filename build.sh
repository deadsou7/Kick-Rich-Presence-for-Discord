#!/bin/bash

# Build script for Kick Stream Monitor
# Creates a standalone Windows executable with all dependencies bundled

echo "Building Kick Stream Monitor..."

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj --configuration Release --verbosity minimal

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj --verbosity minimal

# Build and publish as single file
echo "Building standalone executable..."
dotnet publish KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output ./publish \
    -p:PublishSingleFile=true \
    -p:PublishReadyToRun=true \
    -p:PublishTrimmed=false \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    --verbosity normal

if [ $? -eq 0 ]; then
    echo "✅ Build completed successfully!"
    echo "📦 Executable location: ./publish/KickStatusChecker.Wpf.exe"
    
    # Show file size
    if command -v stat &> /dev/null; then
        SIZE=$(stat -f%z "./publish/KickStatusChecker.Wpf.exe" 2>/dev/null || stat -c%s "./publish/KickStatusChecker.Wpf.exe" 2>/dev/null)
        if [ -n "$SIZE" ]; then
            SIZE_MB=$((SIZE / 1048576))
            echo "📊 File size: ${SIZE_MB}MB"
        fi
    fi
    
    # Create distribution package
    echo "📦 Creating distribution package..."
    mkdir -p ./dist
    cp ./publish/KickStatusChecker.Wpf.exe ./dist/
    cp README.md ./dist/
    
    echo "✅ Distribution package created in ./dist/"
    echo ""
    echo "To create a portable zip:"
    echo "cd dist && zip -r KickStreamMonitor-v1.0.0.zip ."
else
    echo "❌ Build failed!"
    exit 1
fi
