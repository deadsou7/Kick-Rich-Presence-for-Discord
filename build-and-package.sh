#!/bin/bash

# Comprehensive build and packaging script for Kick Stream Monitor
# Creates both portable executable and Windows installer

echo "🚀 Kick Stream Monitor - Build & Package Script"
echo "================================================"

# Configuration
VERSION="1.0.0"
APP_NAME="KickStreamMonitor"
OUTPUT_DIR="./dist"
PUBLISH_DIR="./publish"
INSTALLER_DIR="./installer"

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf "$OUTPUT_DIR" "$PUBLISH_DIR" "$INSTALLER_DIR"
mkdir -p "$OUTPUT_DIR" "$INSTALLER_DIR"

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Build standalone executable
build_executable() {
    echo ""
    echo "📦 Building standalone executable..."
    echo "-----------------------------------"
    
    if ! command_exists dotnet; then
        echo "❌ .NET SDK not found. Please install .NET 8.0 SDK or later."
        return 1
    fi
    
    # Clean and restore
    echo "🔧 Cleaning and restoring dependencies..."
    dotnet clean KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj --configuration Release --verbosity minimal
    dotnet restore KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj --verbosity minimal
    
    # Build and publish
    echo "🏗️  Building executable..."
    dotnet publish KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj \
        --configuration Release \
        --runtime win-x64 \
        --self-contained true \
        --output "$PUBLISH_DIR" \
        -p:PublishSingleFile=true \
        -p:PublishReadyToRun=true \
        -p:PublishTrimmed=false \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:EnableCompressionInSingleFile=true \
        --verbosity normal
    
    if [ $? -eq 0 ]; then
        echo "✅ Executable build completed successfully!"
        
        # Show file size
        EXE_PATH="$PUBLISH_DIR/KickStatusChecker.Wpf.exe"
        if [ -f "$EXE_PATH" ]; then
            SIZE=$(stat -f%z "$EXE_PATH" 2>/dev/null || stat -c%s "$EXE_PATH" 2>/dev/null)
            if [ -n "$SIZE" ]; then
                SIZE_MB=$((SIZE / 1048576))
                echo "📊 File size: ${SIZE_MB}MB"
            fi
        fi
        return 0
    else
        echo "❌ Executable build failed!"
        return 1
    fi
}

# Create portable package
create_portable_package() {
    echo ""
    echo "📁 Creating portable package..."
    echo "-------------------------------"
    
    PORTABLE_DIR="$OUTPUT_DIR/${APP_NAME}-Portable-v${VERSION}"
    mkdir -p "$PORTABLE_DIR"
    
    # Copy executable
    cp "$PUBLISH_DIR/KickStatusChecker.Wpf.exe" "$PORTABLE_DIR/"
    
    # Copy documentation
    cp DISTRIBUTION_README.md "$PORTABLE_DIR/README.md"
    
    # Create version info file
    cat > "$PORTABLE_DIR/VERSION.txt" << EOF
Kick Stream Monitor v${VERSION}
Build Date: $(date)
Platform: Windows x64
Type: Portable (No installation required)
EOF
    
    # Create portable zip
    cd "$OUTPUT_DIR"
    zip -r "${APP_NAME}-Portable-v${VERSION}.zip" "${APP_NAME}-Portable-v${VERSION}"
    cd - > /dev/null
    
    echo "✅ Portable package created: $OUTPUT_DIR/${APP_NAME}-Portable-v${VERSION}.zip"
}

# Create installer (if WiX is available)
create_installer() {
    echo ""
    echo "🔧 Creating Windows installer..."
    echo "--------------------------------"
    
    if ! command_exists dotnet; then
        echo "❌ .NET SDK not found. Skipping installer creation."
        return 1
    fi
    
    # Check if WiX tools are available
    if ! dotnet tool list --global | grep -q wix; then
        echo "📦 Installing WiX Toolset..."
        dotnet tool install --global wix --version 4.0.0
        
        # Add to PATH
        export PATH="$PATH:$HOME/.dotnet/tools"
    fi
    
    # Build installer project
    echo "🏗️  Building installer..."
    cd KickStatusChecker.Wpf
    
    dotnet build KickStatusChecker.Wpf.Setup.wixproj --configuration Release --output "../$INSTALLER_DIR"
    
    if [ $? -eq 0 ]; then
        echo "✅ Installer created successfully!"
        
        # Copy installer to output directory
        find "$INSTALLER_DIR" -name "*.msi" -exec cp {} "../$OUTPUT_DIR/${APP_NAME}-Setup-v${VERSION}.msi" \;
        
        echo "📦 Installer location: $OUTPUT_DIR/${APP_NAME}-Setup-v${VERSION}.msi"
    else
        echo "❌ Installer build failed!"
        echo "💡 You can still use the portable version"
    fi
    
    cd - > /dev/null
}

# Create checksums
create_checksums() {
    echo ""
    echo "🔐 Creating checksums..."
    echo "------------------------"
    
    cd "$OUTPUT_DIR"
    
    # Create SHA256 checksums
    sha256sum *.zip *.msi 2>/dev/null > checksums.txt
    
    echo "✅ Checksums created: $OUTPUT_DIR/checksums.txt"
    
    # Display checksums
    echo ""
    echo "📋 File checksums:"
    cat checksums.txt
    
    cd - > /dev/null
}

# Display summary
display_summary() {
    echo ""
    echo "🎉 Build completed successfully!"
    echo "================================="
    echo ""
    echo "📦 Output files created in: $OUTPUT_DIR"
    echo ""
    
    # List created files
    if [ -d "$OUTPUT_DIR" ]; then
        echo "📋 Created files:"
        ls -lh "$OUTPUT_DIR" | grep -v "^total"
    fi
    
    echo ""
    echo "📖 Installation Instructions:"
    echo "------------------------------"
    echo "📁 Portable Version:"
    echo "   1. Download ${APP_NAME}-Portable-v${VERSION}.zip"
    echo "   2. Extract to any folder"
    echo "   3. Run KickStatusChecker.Wpf.exe"
    echo ""
    echo "🔧 Installer Version:"
    echo "   1. Download ${APP_NAME}-Setup-v${VERSION}.msi"
    echo "   2. Run the installer"
    echo "   3. Follow the installation wizard"
    echo ""
    echo "📖 For detailed instructions, see README.md in the package"
}

# Main execution
main() {
    echo "Starting build process..."
    
    # Build executable
    if build_executable; then
        # Create portable package
        create_portable_package
        
        # Create installer (optional)
        create_installer
        
        # Create checksums
        create_checksums
        
        # Display summary
        display_summary
        
        echo ""
        echo "✅ All done! Your Kick Stream Monitor is ready for distribution."
    else
        echo ""
        echo "❌ Build process failed. Please check the error messages above."
        exit 1
    fi
}

# Run main function
main "$@"
