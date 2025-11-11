#!/bin/bash

# Test script to verify build process and output
# This script verifies the build artifacts without requiring .NET

echo "🧪 Kick Stream Monitor - Build Verification"
echo "=========================================="

# Configuration
VERSION="1.0.0"
APP_NAME="KickStreamMonitor"
EXPECTED_FILES=("KickStatusChecker.Wpf.exe" "README.md")

# Function to check if file exists
check_file() {
    local file="$1"
    local description="$2"
    
    if [ -f "$file" ]; then
        echo "✅ $description: $file"
        
        # Show file size
        if command -v stat &> /dev/null; then
            SIZE=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file" 2>/dev/null)
            if [ -n "$SIZE" ]; then
                SIZE_MB=$((SIZE / 1048576))
                echo "   📊 Size: ${SIZE_MB}MB (${SIZE} bytes)"
            fi
        fi
        
        return 0
    else
        echo "❌ $description: $file (not found)"
        return 1
    fi
}

# Function to check directory structure
check_directory() {
    local dir="$1"
    local description="$2"
    
    if [ -d "$dir" ]; then
        echo "✅ $description: $dir"
        
        # List contents
        echo "   📁 Contents:"
        ls -la "$dir" | grep -v "^total" | grep -v "^\." | head -10
        return 0
    else
        echo "❌ $description: $dir (not found)"
        return 1
    fi
}

# Function to verify portable package
verify_portable_package() {
    echo ""
    echo "📦 Verifying Portable Package"
    echo "------------------------------"
    
    local portable_dir="$OUTPUT_DIR/${APP_NAME}-Portable-v${VERSION}"
    
    if check_directory "$portable_dir" "Portable directory"; then
        echo ""
        echo "📋 Checking required files:"
        
        local all_files_exist=true
        for file in "${EXPECTED_FILES[@]}"; do
            if ! check_file "$portable_dir/$file" "Required file"; then
                all_files_exist=false
            fi
        done
        
        if [ "$all_files_exist" = true ]; then
            echo "✅ All required files present in portable package"
        else
            echo "❌ Some required files missing from portable package"
        fi
        
        # Check VERSION.txt
        if [ -f "$portable_dir/VERSION.txt" ]; then
            echo "✅ VERSION.txt found"
            echo "   📄 Content:"
            cat "$portable_dir/VERSION.txt" | head -5
        fi
    fi
}

# Function to verify installer
verify_installer() {
    echo ""
    echo "🔧 Verifying Installer"
    echo "----------------------"
    
    local installer="$OUTPUT_DIR/${APP_NAME}-Setup-v${VERSION}.msi"
    
    if check_file "$installer" "Installer file"; then
        # Basic MSI file check (MSI files have specific header)
        if command -v file &> /dev/null; then
            file_type=$(file "$installer")
            echo "   📄 File type: $file_type"
        fi
        
        # Check file size (MSI files should be reasonable size)
        if command -v stat &> /dev/null; then
            SIZE=$(stat -f%z "$installer" 2>/dev/null || stat -c%s "$installer" 2>/dev/null)
            if [ -n "$SIZE" ]; then
                SIZE_MB=$((SIZE / 1048576))
                if [ $SIZE_MB -gt 10 ] && [ $SIZE_MB -lt 200 ]; then
                    echo "✅ Installer size looks reasonable: ${SIZE_MB}MB"
                else
                    echo "⚠️  Installer size might be unusual: ${SIZE_MB}MB"
                fi
            fi
        fi
    fi
}

# Function to verify checksums
verify_checksums() {
    echo ""
    echo "🔐 Verifying Checksums"
    echo "----------------------"
    
    local checksums_file="$OUTPUT_DIR/checksums.txt"
    
    if check_file "$checksums_file" "Checksums file"; then
        echo "   📋 Checksum entries:"
        wc -l "$checksums_file" | awk '{print "   📊 " $1 " entries"}'
        
        echo ""
        echo "   📄 Sample checksums:"
        head -3 "$checksums_file" | while read line; do
            echo "   $line"
        done
        
        # Verify checksum format (SHA256 + filename)
        local valid_checksums=true
        while IFS= read -r line; do
            if [[ ! "$line" =~ ^[a-f0-9]{64}\s+.+$ ]]; then
                echo "❌ Invalid checksum format: $line"
                valid_checksums=false
            fi
        done < "$checksums_file"
        
        if [ "$valid_checksums" = true ]; then
            echo "✅ Checksum format looks valid"
        fi
    fi
}

# Function to verify zip file
verify_zip_file() {
    echo ""
    echo "📦 Verifying ZIP File"
    echo "---------------------"
    
    local zip_file="$OUTPUT_DIR/${APP_NAME}-Portable-v${VERSION}.zip"
    
    if check_file "$zip_file" "ZIP file"; then
        # Check if it's a valid ZIP file
        if command -v unzip &> /dev/null; then
            if unzip -t "$zip_file" > /dev/null 2>&1; then
                echo "✅ ZIP file is valid and can be extracted"
                
                # List contents
                echo "   📁 ZIP contents:"
                unzip -l "$zip_file" | tail -10
            else
                echo "❌ ZIP file is corrupted or invalid"
            fi
        elif command -v file &> /dev/null; then
            file_type=$(file "$zip_file")
            echo "   📄 File type: $file_type"
        fi
    fi
}

# Main verification
main() {
    # Set output directory
    OUTPUT_DIR="./dist"
    
    echo "Checking build artifacts in: $OUTPUT_DIR"
    echo ""
    
    # Check if output directory exists
    if [ ! -d "$OUTPUT_DIR" ]; then
        echo "❌ Output directory not found: $OUTPUT_DIR"
        echo "💡 Run the build script first: ./build-and-package.sh"
        exit 1
    fi
    
    # Verify build artifacts
    verify_portable_package
    verify_installer
    verify_zip_file
    verify_checksums
    
    echo ""
    echo "🎉 Verification Complete!"
    echo "========================="
    echo ""
    echo "📊 Summary:"
    
    # Count successful checks
    local total_files=0
    local found_files=0
    
    # Count files in output directory
    if [ -d "$OUTPUT_DIR" ]; then
        total_files=$(find "$OUTPUT_DIR" -type f | wc -l)
        echo "   📁 Total files created: $total_files"
    fi
    
    # Check for key artifacts
    local artifacts=(
        "${APP_NAME}-Portable-v${VERSION}.zip"
        "${APP_NAME}-Setup-v${VERSION}.msi"
        "checksums.txt"
    )
    
    echo "   📦 Key artifacts:"
    for artifact in "${artifacts[@]}"; do
        if [ -f "$OUTPUT_DIR/$artifact" ]; then
            echo "   ✅ $artifact"
            ((found_files++))
        else
            echo "   ❌ $artifact"
        fi
    done
    
    echo ""
    if [ $found_files -eq ${#artifacts[@]} ]; then
        echo "🎉 All key artifacts found! Build verification successful."
    else
        echo "⚠️  Some artifacts missing. Check the build output above."
    fi
}

# Run verification
main "$@"
