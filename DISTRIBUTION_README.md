# Kick Stream Monitor - Desktop Application

A standalone Windows desktop application for monitoring Kick stream status in real-time.

## 🚀 Quick Start

### Download & Run (Portable Version)

1. Download the latest `KickStreamMonitor-v1.0.0.zip` from the [Releases](https://github.com/your-repo/kick-status-checker/releases) page
2. Extract the zip file to any folder
3. Double-click `KickStatusChecker.Wpf.exe` to run
4. No installation required - works immediately!

### System Requirements

- **Windows 10 Version 1809** or later (Build 17763)
- **Windows 11** (all versions)
- **Architecture**: x64 (64-bit)
- **Memory**: 512MB RAM minimum
- **Storage**: 100MB free space
- **.NET Runtime**: ✅ **Not required** - application is self-contained

## 📋 Features

- ✅ **Real-time monitoring** of Kick stream status
- ✅ **System tray integration** with minimize to tray support
- ✅ **Customizable update intervals** (10-30 seconds)
- ✅ **Multiple display modes** for status information
- ✅ **Persistent settings** saved automatically
- ✅ **Offline detection** with error handling
- ✅ **Lightweight and fast** - minimal resource usage
- ✅ **No external dependencies** - completely standalone

## 🎯 How to Use

### Basic Setup

1. **Launch the application** by running `KickStatusChecker.Wpf.exe`
2. **Enter a Kick username** in the text field (e.g., "xqc", "destiny", "trainwreck")
3. **Click "Start Monitoring"** to begin tracking the stream status
4. **Minimize to tray** to keep it running in the background

### Settings Configuration

- **Update Interval**: Choose how often to check the stream status (10-30 seconds)
- **Minimize to Tray**: Enable to hide the application in the system tray when minimized
- **Status Display Mode**: Select how status information is displayed:
  - **Live/Offline**: Simple online/offline status
  - **Detailed**: Shows title, category, and last update time
  - **Compact**: Minimal information display

### System Tray Features

- Right-click the tray icon for quick options
- Double-click to restore the main window
- Status changes are reflected in the tray icon tooltip

## 🔧 Troubleshooting

### Common Issues

**Application won't start:**
- Ensure you're running Windows 10 1809+ or Windows 11
- Try running as Administrator
- Check that your antivirus isn't blocking the executable

**"User not found" error:**
- Verify the Kick username is spelled correctly
- Check that the user exists on kick.com/{username}
- Usernames are case-insensitive but must be exact

**Status not updating:**
- Check your internet connection
- Try increasing the update interval
- Kick.com might be experiencing temporary issues

**Application uses too much CPU:**
- Increase the update interval to 20-30 seconds
- Close other applications that might be using network resources

### Error Messages

- **"Network error occurred"**: Check your internet connection
- **"Invalid username"**: Username contains invalid characters or is empty
- **"Rate limit exceeded"**: Wait a few minutes before trying again

## 🎮 Discord Developer Portal Setup (Optional)

If you want to integrate Discord Rich Presence or create a Discord bot:

### 1. Create Discord Application

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Click **"New Application"**
3. Enter an application name (e.g., "Kick Stream Monitor")
4. Agree to the terms and click **"Create"**

### 2. Get Application ID

1. In your application dashboard, click **"General Information"**
2. Copy the **Application ID** (this is your Client ID)
3. This ID is used for Discord Rich Presence integration

### 3. Configure Rich Presence (Optional)

1. Click **"Rich Presence"** in the left menu
2. Upload application icons (optional)
3. Configure Rich Presence assets
4. Add the Application ID to your application settings

### 4. Add Bot (Optional)

1. Click **"Bot"** in the left menu
2. Click **"Add Bot"** and confirm
3. Copy the Bot Token (keep this secret!)
4. Enable necessary bot permissions:
   - **Read Messages/View Channels**
   - **Send Messages**
   - **Embed Links**

### 5. Invite Bot to Server

1. Click **"OAuth2"** → **"URL Generator"**
2. Select bot scopes and permissions
3. Copy the generated URL
4. Paste in browser and invite to your Discord server

## 📦 Installation Options

### Portable Version (Recommended)
- **File**: `KickStreamMonitor-v1.0.0.zip`
- **Installation**: Just extract and run
- **Best for**: Quick testing, multiple computers, USB drives

### Installer Version
- **File**: `KickStreamMonitor-Setup-v1.0.0.exe`
- **Installation**: Run installer and follow wizard
- **Features**: 
  - Start Menu shortcut
  - Desktop shortcut (optional)
  - Auto-start with Windows (optional)
  - Add/Remove Programs entry

## 🔄 Updates

### Checking for Updates
- Current version: 1.0.0
- Check the [GitHub Releases](https://github.com/your-repo/kick-status-checker/releases) page for updates
- Subscribe to release notifications

### Updating Portable Version
1. Download the latest version
2. Close the running application
3. Extract new version over old files (or to new folder)
4. Launch the new version

### Updating Installed Version
1. Download the latest installer
2. Run the installer (it will upgrade automatically)
3. Your settings will be preserved

## 📁 File Locations

### Settings File
- **Location**: `%APPDATA%\KickStatusChecker\settings.json`
- **Auto-created**: First time you run the application
- **Contains**: Username, update interval, display preferences, tray settings

### Log Files
- **Location**: `%LOCALAPPDATA%\KickStatusChecker\logs\`
- **Files**: Error logs and debug information
- **Used for**: Troubleshooting application issues

## 🔒 Privacy & Security

### Data Collection
- ✅ **No personal data collected**
- ✅ **No telemetry or analytics**
- ✅ **No internet usage except for Kick.com API**
- ✅ **Settings stored locally only**

### Network Access
- **Only connects to**: `https://kick.com/{username}`
- **No data sent**: Only requests public stream information
- **Cached requests**: 12-second cache to reduce API calls

## 🛠️ Technical Details

### Build Information
- **Framework**: .NET 8.0 (self-contained)
- **Architecture**: x64 (64-bit)
- **Type**: Single-file executable
- **Compilation**: ReadyToRun enabled for faster startup
- **Size**: ~40-50MB (includes .NET runtime)

### Dependencies
- **None required** - completely self-contained
- **Includes**: .NET 8.0 runtime, Windows Forms, WPF
- **External**: HtmlAgilityPack (embedded), System.Text.Json (built-in)

## 📞 Support

### Getting Help
- **GitHub Issues**: [Report bugs and request features](https://github.com/your-repo/kick-status-checker/issues)
- **Discord**: Join our community server (link in README)
- **Documentation**: Check the [Wiki](https://github.com/your-repo/kick-status-checker/wiki)

### Contributing
We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This application is open source and available under the [MIT License](LICENSE).

---

**Version**: 1.0.0  
**Last Updated**: 2024  
**Compatibility**: Windows 10 1809+ / Windows 11
