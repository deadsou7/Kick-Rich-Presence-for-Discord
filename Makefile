# Makefile for Kick Stream Monitor
# Cross-platform build and distribution system

# Configuration
VERSION := 1.0.0
APP_NAME := KickStreamMonitor
OUTPUT_DIR := dist
PUBLISH_DIR := publish
INSTALLER_DIR := installer
PROJECT := KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj

# Default target
.PHONY: help
help: ## Show this help message
	@echo "Kick Stream Monitor - Build System"
	@echo "=================================="
	@echo ""
	@echo "Available targets:"
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  %-20s %s\n", $$1, $$2}' $(MAKEFILE_LIST)
	@echo ""
	@echo "Examples:"
	@echo "  make build          # Build executable"
	@echo "  make package        # Create distribution package"
	@echo "  make all            # Build everything"
	@echo "  make clean          # Clean build artifacts"

# Check if .NET is available
.PHONY: check-dotnet
check-dotnet:
	@if ! command -v dotnet >/dev/null 2>&1; then \
		echo "❌ .NET SDK not found. Please install .NET 8.0 SDK or later."; \
		echo "   Download from: https://dotnet.microsoft.com/download"; \
		exit 1; \
	fi
	@echo "✅ .NET SDK found: $$(dotnet --version)"

# Clean build artifacts
.PHONY: clean
clean: ## Clean all build artifacts
	@echo "🧹 Cleaning build artifacts..."
	@rm -rf $(OUTPUT_DIR) $(PUBLISH_DIR) $(INSTALLER_DIR)
	@dotnet clean $(PROJECT) --configuration Release --verbosity minimal 2>/dev/null || true
	@echo "✅ Clean completed"

# Restore dependencies
.PHONY: restore
restore: check-dotnet ## Restore NuGet dependencies
	@echo "📦 Restoring dependencies..."
	@dotnet restore $(PROJECT) --verbosity minimal
	@echo "✅ Dependencies restored"

# Build executable
.PHONY: build
build: check-dotnet restore ## Build standalone executable
	@echo "🏗️  Building standalone executable..."
	@dotnet publish $(PROJECT) \
		--configuration Release \
		--runtime win-x64 \
		--self-contained true \
		--output $(PUBLISH_DIR) \
		-p:PublishSingleFile=true \
		-p:PublishReadyToRun=true \
		-p:PublishTrimmed=false \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:EnableCompressionInSingleFile=true \
		--verbosity normal
	@if [ -f "$(PUBLISH_DIR)/KickStatusChecker.Wpf.exe" ]; then \
		SIZE=$$(stat -f%z "$(PUBLISH_DIR)/KickStatusChecker.Wpf.exe" 2>/dev/null || stat -c%s "$(PUBLISH_DIR)/KickStatusChecker.Wpf.exe" 2>/dev/null); \
		SIZE_MB=$$((SIZE / 1048576)); \
		echo "✅ Build completed! File size: $${SIZE_MB}MB"; \
	else \
		echo "❌ Build failed!"; \
		exit 1; \
	fi

# Create portable package
.PHONY: portable
portable: build ## Create portable package
	@echo "📦 Creating portable package..."
	@mkdir -p $(OUTPUT_DIR)
	@PORTABLE_DIR="$(OUTPUT_DIR)/$(APP_NAME)-Portable-v$(VERSION)"; \
	mkdir -p $$PORTABLE_DIR; \
	cp "$(PUBLISH_DIR)/KickStatusChecker.Wpf.exe" "$$PORTABLE_DIR/"; \
	cp "DISTRIBUTION_README.md" "$$PORTABLE_DIR/README.md"; \
	echo "Kick Stream Monitor v$(VERSION)" > "$$PORTABLE_DIR/VERSION.txt"; \
	echo "Build Date: $$(date)" >> "$$PORTABLE_DIR/VERSION.txt"; \
	echo "Platform: Windows x64" >> "$$PORTABLE_DIR/VERSION.txt"; \
	echo "Type: Portable (No installation required)" >> "$$PORTABLE_DIR/VERSION.txt"; \
	cd $(OUTPUT_DIR) && zip -r "$(APP_NAME)-Portable-v$(VERSION).zip" "$(APP_NAME)-Portable-v$(VERSION)"; \
	echo "✅ Portable package created: $(OUTPUT_DIR)/$(APP_NAME)-Portable-v$(VERSION).zip"

# Create installer (if WiX is available)
.PHONY: installer
installer: build ## Create Windows installer (requires WiX)
	@echo "🔧 Creating Windows installer..."
	@if ! dotnet tool list --global | grep -q wix; then \
		echo "📦 Installing WiX Toolset..."; \
		dotnet tool install --global wix --version 4.0.0; \
	fi
	@mkdir -p $(INSTALLER_DIR)
	@cd KickStatusChecker.Wpf && \
	dotnet build KickStatusChecker.Wpf.Setup.wixproj --configuration Release --output "../$(INSTALLER_DIR)" && \
	cd .. && \
	find $(INSTALLER_DIR) -name "*.msi" -exec cp {} "$(OUTPUT_DIR)/$(APP_NAME)-Setup-v$(VERSION).msi" \; && \
	echo "✅ Installer created: $(OUTPUT_DIR)/$(APP_NAME)-Setup-v$(VERSION).msi" || \
	echo "⚠️  Installer creation failed. Portable version is still available."

# Create checksums
.PHONY: checksums
checksums: ## Create SHA256 checksums for all artifacts
	@echo "🔐 Creating checksums..."
	@cd $(OUTPUT_DIR) && \
	if ls *.zip *.msi >/dev/null 2>&1; then \
		sha256sum *.zip *.msi > checksums.txt && \
		echo "✅ Checksums created: $(OUTPUT_DIR)/checksums.txt" && \
		echo "📋 Checksums:" && \
		cat checksums.txt; \
	else \
		echo "⚠️  No files found for checksum creation"; \
	fi

# Create complete distribution package
.PHONY: package
package: portable installer checksums ## Create complete distribution package
	@echo "🎉 Distribution package created!"

# Build everything
.PHONY: all
all: package ## Build everything (executable + packages)

# Quick build (executable only)
.PHONY: quick
quick: build ## Quick build (executable only)

# Verify build artifacts
.PHONY: verify
verify: ## Verify build artifacts
	@echo "🧪 Verifying build artifacts..."
	@if [ -f "$(OUTPUT_DIR)/$(APP_NAME)-Portable-v$(VERSION).zip" ]; then \
		echo "✅ Portable package found"; \
	else \
		echo "❌ Portable package missing"; \
	fi
	@if [ -f "$(OUTPUT_DIR)/$(APP_NAME)-Setup-v$(VERSION).msi" ]; then \
		echo "✅ Installer found"; \
	else \
		echo "⚠️  Installer not found (optional)"; \
	fi
	@if [ -f "$(OUTPUT_DIR)/checksums.txt" ]; then \
		echo "✅ Checksums found"; \
	else \
		echo "⚠️  Checksums missing"; \
	fi

# Show build information
.PHONY: info
info: ## Show build configuration
	@echo "📋 Build Configuration"
	@echo "====================="
	@echo "Version: $(VERSION)"
	@echo "App Name: $(APP_NAME)"
	@echo "Output Directory: $(OUTPUT_DIR)"
	@echo "Project: $(PROJECT)"
	@echo ""
	@if command -v dotnet >/dev/null 2>&1; then \
		echo "✅ .NET SDK: $$(dotnet --version)"; \
	else \
		echo "❌ .NET SDK: Not found"; \
	fi
	@if dotnet tool list --global | grep -q wix; then \
		echo "✅ WiX Toolset: Available"; \
	else \
		echo "⚠️  WiX Toolset: Not available (installer creation will fail)"; \
	fi

# Install build dependencies
.PHONY: install-deps
install-deps: ## Install build dependencies
	@echo "📦 Installing build dependencies..."
	@if ! command -v dotnet >/dev/null 2>&1; then \
		echo "❌ Please install .NET 8.0 SDK manually:"; \
		echo "   https://dotnet.microsoft.com/download"; \
		exit 1; \
	fi
	@if ! dotnet tool list --global | grep -q wix; then \
		echo "📦 Installing WiX Toolset..."; \
		dotnet tool install --global wix --version 4.0.0; \
	else \
		echo "✅ WiX Toolset already installed"; \
	fi
	@echo "✅ Dependencies installed"

# Development setup
.PHONY: setup
setup: install-deps ## Set up development environment
	@echo "🔧 Setting up development environment..."
	@dotnet restore $(PROJECT)
	@echo "✅ Development environment ready"

# Run tests
.PHONY: test
test: ## Run all tests
	@echo "🧪 Running tests..."
	@dotnet test KickStatusChecker.Tests/KickStatusChecker.Tests.csproj --verbosity normal

# Start development server
.PHONY: dev
dev: ## Run WPF application in development mode
	@echo "🚀 Starting development mode..."
	@dotnet run --project KickStatusChecker.Wpf/KickStatusChecker.Wpf.csproj

# Watch for changes and rebuild
.PHONY: watch
watch: ## Watch for changes and rebuild
	@echo "👀 Watching for changes..."
	@echo "📝 Not implemented yet - use 'make dev' for manual testing"

# Show file sizes
.PHONY: sizes
sizes: ## Show file sizes of build artifacts
	@echo "📊 File Sizes"
	@echo "============="
	@if [ -d "$(OUTPUT_DIR)" ]; then \
		cd $(OUTPUT_DIR) && \
		for file in *; do \
			if [ -f "$$file" ]; then \
				SIZE=$$(stat -f%z "$$file" 2>/dev/null || stat -c%s "$$file" 2>/dev/null); \
				SIZE_MB=$$((SIZE / 1048576)); \
				printf "%-40s %8d MB (%d bytes)\n" "$$file" "$$SIZE_MB" "$$SIZE"; \
			fi; \
		done; \
	else \
		echo "No build artifacts found. Run 'make package' first."; \
	fi

# Clean everything including dependencies
.PHONY: clean-all
clean-all: clean ## Clean everything including tool cache
	@echo "🧹 Cleaning tool cache..."
	@dotnet tool uninstall --global wix 2>/dev/null || true
	@echo "✅ Full clean completed"
