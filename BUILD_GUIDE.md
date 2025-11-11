# Build & Distribution Guide

This guide explains how to build and distribute the Kick Stream Monitor desktop application.

## 🏗️ Build System Overview

The project includes multiple build scripts and configurations to create standalone Windows executables and distribution packages.

### Build Scripts

| Script | Platform | Purpose | Features |
|--------|----------|---------|----------|
| `build.sh` | Linux/macOS | Basic executable build | Single file, self-contained |
| `build.ps1` | Windows PowerShell | Basic executable build | Single file, self-contained |
| `quick-build.bat` | Windows CMD | Quick build without dependencies | Simple, no external tools |
| `build-and-package.sh` | Linux/macOS | Complete distribution package | Portable + Installer + Checksums |
| `build-and-package.ps1` | Windows PowerShell | Complete distribution package | Portable + Installer + Checksums |

## 🚀 Quick Build (Windows)

### Option 1: PowerShell (Recommended)
```powershell
.\build-and-package.ps1
```

### Option 2: Command Prompt
```cmd
quick-build.bat
```

### Option 3: Manual .NET CLI
```cmd
dotnet publish KickStatusChecker.Wpf\KickStatusChecker.Wpf.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output publish ^
    -p:PublishSingleFile=true ^
    -p:PublishReadyToRun=true ^
    -p:PublishTrimmed=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true
```

## 📦 Build Configuration

### Key Publishing Properties

- **PublishSingleFile**: Combines all dependencies into a single .exe
- **SelfContained**: Includes .NET runtime (no installation required)
- **ReadyToRun**: Pre-compiles for faster startup
- **EnableCompressionInSingleFile**: Reduces file size
- **IncludeNativeLibrariesForSelfExtract**: Handles native dependencies
- **PublishTrimmed**: Disabled to ensure compatibility

### Target Configuration

- **Target Framework**: .NET 8.0 Windows
- **Runtime**: win-x64 (64-bit Windows)
- **Output Type**: WinExe (Windows executable)
- **UI Framework**: WPF + Windows Forms

## 📁 Distribution Structure

### Portable Package
```
KickStreamMonitor-Portable-v1.0.0/
├── KickStatusChecker.Wpf.exe    # Main executable
├── README.md                     # User documentation
└── VERSION.txt                   # Build information
```

### Installer Package
```
KickStreamMonitor-Setup-v1.0.0.msi  # Windows installer
```

## 🔧 Automated Builds

### GitHub Actions Workflow

The project includes a GitHub Actions workflow (`.github/workflows/build-and-release.yml`) that:

1. **Triggers** on:
   - Git tags starting with `v*` (e.g., `v1.0.0`)
   - Manual workflow dispatch

2. **Builds** on Windows using:
   - .NET 8.0 SDK
   - Release configuration
   - win-x64 runtime

3. **Creates**:
   - Portable zip package
   - SHA256 checksums
   - GitHub release with automatic changelog

4. **Publishes**:
   - Build artifacts (30-day retention)
   - GitHub release with downloadable files

### Manual Workflow Trigger

You can trigger builds manually via GitHub Actions:

1. Go to Actions → Build and Release
2. Click "Run workflow"
3. Enter version number (e.g., `1.0.0`)
4. Choose whether to create a GitHub release
5. Click "Run workflow"

## 🎯 Build Requirements

### Development Machine
- **.NET 8.0 SDK** or later
- **Windows 10+** (for building Windows applications)
- **Git** (for version control)

### Optional for Installer Creation
- **WiX Toolset v4.0** (automatically installed by scripts)
- **PowerShell 5.1+** (for advanced build scripts)

### Runtime Requirements (End Users)
- **Windows 10 Version 1809+** or **Windows 11**
- **x64 architecture**
- **No .NET installation required** (self-contained)

## 🔍 Build Verification

### File Size Targets
- **Executable**: ~40-50MB (includes .NET runtime)
- **Portable Zip**: ~45-55MB
- **Installer**: ~45-55MB

### Functional Testing Checklist

- [ ] Application launches without errors
- [ ] Can enter Kick username and start monitoring
- [ ] Status updates correctly
- [ ] Settings persist after restart
- [ ] System tray functionality works
- [ ] Minimize to tray works
- [ ] Update interval changes take effect
- [ ] Display modes work correctly
- [ ] Error handling displays properly

### Compatibility Testing

Test on clean Windows installations:
- [ ] Windows 10 1809 (Build 17763)
- [ ] Windows 10 21H2 (Build 19044)
- [ ] Windows 11 21H2 (Build 22000)
- [ ] Windows 11 22H2 (Build 22621)

## 📋 Release Process

### 1. Version Management
1. Update version numbers in:
   - `KickStatusChecker.Wpf.csproj` (AssemblyVersion/FileVersion)
   - Build scripts (if needed)

2. Commit changes:
   ```bash
   git add .
   git commit -m "Bump version to 1.0.0"
   ```

### 2. Create Release Tag
```bash
git tag v1.0.0
git push origin v1.0.0
```

### 3. Automated Build
- GitHub Actions will automatically trigger
- Build creates portable package and checksums
- GitHub release is created with download links

### 4. Manual Build (Optional)
```powershell
.\build-and-package.ps1 -Version 1.0.0
```

## 🐛 Troubleshooting

### Common Build Issues

**"dotnet command not found"**
- Install .NET 8.0 SDK from https://dotnet.microsoft.com/download
- Restart terminal/command prompt
- Verify with `dotnet --version`

**"Access denied" errors**
- Run terminal as Administrator
- Check antivirus isn't blocking the build
- Ensure write permissions to project directory

**"WiX tool not found"**
- Build scripts automatically install WiX
- Manual install: `dotnet tool install --global wix --version 4.0.0`
- Add `%USERPROFILE%\.dotnet\tools` to PATH

**Large file size**
- Verify `PublishTrimmed` is set to `false` (required for compatibility)
- Check `EnableCompressionInSingleFile` is `true`
- Consider removing unused dependencies

### Runtime Issues

**Application won't start**
- Verify Windows version (10 1809+)
- Check antivirus isn't blocking executable
- Try running as Administrator
- Check Event Viewer for .NET runtime errors

**Missing dependencies**
- Ensure self-contained publish was used
- Verify all files are included in distribution
- Test on clean Windows installation

## 📊 Performance Optimization

### Build Time Optimization
- Use `--verbosity minimal` for cleaner output
- Enable incremental builds during development
- Use `dotnet build --configuration Release` for production builds

### Runtime Optimization
- ReadyToRun compilation reduces startup time
- Single-file reduces disk I/O
- Compression reduces download size
- No trimming ensures maximum compatibility

## 🔄 Continuous Integration

### Local Development Workflow

1. **Make changes** to source code
2. **Build locally**: `.\build-and-package.ps1`
3. **Test functionality** on clean machine
4. **Commit changes**: `git commit -m "Description"`
5. **Push to repository**: `git push`
6. **Create tag**: `git tag v1.0.1 && git push origin v1.0.1`
7. **Monitor GitHub Actions** build
8. **Download artifacts** from Actions tab
9. **Test release artifacts** thoroughly
10. **Publish GitHub release** (automatic or manual)

### Branch Strategy

- **main**: Stable releases
- **develop**: Integration branch
- **feature/***: Feature development
- **release/***: Release preparation
- **hotfix/***: Emergency fixes

## 📝 Additional Resources

- [.NET Publishing Guide](https://docs.microsoft.com/en-us/dotnet/core/deploying/)
- [WPF Deployment](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/app-development/deployment)
- [WiX Toolset Documentation](https://wixtoolset.org/docs/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
