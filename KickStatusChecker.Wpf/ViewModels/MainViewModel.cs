using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KickStatusChecker.Wpf.Commands;
using KickStatusChecker.Wpf.Models;
using KickStatusChecker.Wpf.Services;

namespace KickStatusChecker.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private const int MinimumIntervalSeconds = 10;
    private const int MaximumIntervalSeconds = 30;
    private static readonly Regex UsernameRegex = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    private readonly global::KickStatusChecker.KickStatusChecker _statusChecker;
    private readonly SettingsService _settingsService;
    private readonly MonitoringService _monitoringService;
    private readonly DiscordPresenceManager _discordPresenceManager;
    private readonly SynchronizationContext? _syncContext;
    private readonly RelayCommand _toggleMonitoringCommand;
    private readonly RelayCommand _exitCommand;
    private readonly RelayCommand _minimizeCommand;

    private bool _disposed;
    private bool _isInitializingSettings;

    private string _username = string.Empty;
    private string _usernameValidationMessage = string.Empty;
    private bool _isMonitoring;
    private string _streamTitle = string.Empty;
    private string _category = string.Empty;
    private bool _isLive;
    private DateTime? _lastUpdated;
    private string _errorMessage = string.Empty;
    private int _updateIntervalSeconds = 15;
    private bool _minimizeToTray = true;
    private StatusDisplayMode _statusDisplayMode = StatusDisplayMode.Actual;
    private string _currentStatusText = "Not monitoring";
    private string _statusMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? RequestExit;
    public event EventHandler? RequestMinimize;

    public IReadOnlyList<StatusDisplayOption> StatusDisplayModes { get; }

    public MainViewModel()
    {
        Logger.LogInfo("Initializing MainViewModel");
        
        _syncContext = SynchronizationContext.Current;
        _statusChecker = new global::KickStatusChecker.KickStatusChecker();
        _discordPresenceManager = new DiscordPresenceManager();
        _monitoringService = new MonitoringService(_statusChecker, _discordPresenceManager, _syncContext);
        _settingsService = new SettingsService();
        
        StatusDisplayModes = new[]
        {
            new StatusDisplayOption("Actual Status", StatusDisplayMode.Actual),
            new StatusDisplayOption("Force Online", StatusDisplayMode.ForceOnline),
            new StatusDisplayOption("Force Offline", StatusDisplayMode.ForceOffline)
        };

        _toggleMonitoringCommand = new RelayCommand(ToggleMonitoring, CanToggleMonitoring);
        _exitCommand = new RelayCommand(() => RequestExit?.Invoke(this, EventArgs.Empty));
        _minimizeCommand = new RelayCommand(() => RequestMinimize?.Invoke(this, EventArgs.Empty));

        // Subscribe to monitoring service events
        _monitoringService.StreamStatusUpdated += OnStreamStatusUpdated;
        _monitoringService.StatusMessageUpdated += OnStatusMessageUpdated;
        _monitoringService.ErrorOccurred += OnErrorOccurred;
        _monitoringService.MonitoringStarted += OnMonitoringStarted;
        _monitoringService.MonitoringStopped += OnMonitoringStopped;

        LoadSettings();
        ValidateUsername();
        UpdateStatusDisplay();
        
        Logger.LogInfo("MainViewModel initialized successfully");
    }

    public RelayCommand ToggleMonitoringCommand => _toggleMonitoringCommand;
    public RelayCommand ExitCommand => _exitCommand;
    public RelayCommand MinimizeCommand => _minimizeCommand;

    public string Username
    {
        get => _username;
        set
        {
            var sanitized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _username, sanitized))
            {
                ValidateUsername();
                if (!_isInitializingSettings)
                {
                    SaveSettings();
                }
                _toggleMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string UsernameValidationMessage
    {
        get => _usernameValidationMessage;
        private set => SetProperty(ref _usernameValidationMessage, value);
    }

    public bool IsMonitoring
    {
        get => _monitoringService.IsMonitoring;
        private set
        {
            if (SetProperty(ref _isMonitoring, value))
            {
                OnPropertyChanged(nameof(StartStopButtonText));
                _toggleMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StartStopButtonText => IsMonitoring ? "Stop" : "Start";

    public string StreamTitle
    {
        get => string.IsNullOrWhiteSpace(_streamTitle) ? "N/A" : _streamTitle;
        private set => SetProperty(ref _streamTitle, value);
    }

    public string Category
    {
        get => string.IsNullOrWhiteSpace(_category) ? "N/A" : _category;
        private set => SetProperty(ref _category, value);
    }

    public bool IsLive
    {
        get => _isLive;
        private set => SetProperty(ref _isLive, value);
    }

    public string CurrentStatusText
    {
        get => _currentStatusText;
        private set
        {
            if (SetProperty(ref _currentStatusText, value))
            {
                OnPropertyChanged(nameof(DisplayedStatusText));
            }
        }
    }

    public string DisplayedStatusText => StatusDisplayMode switch
    {
        StatusDisplayMode.ForceOnline => "Online (forced)",
        StatusDisplayMode.ForceOffline => "Offline (forced)",
        _ => CurrentStatusText
    };

    public string LastUpdatedDisplay => _lastUpdated.HasValue
        ? _lastUpdated.Value.ToLocalTime().ToString("g")
        : "Never";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public int UpdateIntervalSeconds
    {
        get => _updateIntervalSeconds;
        set
        {
            var clamped = Math.Clamp(value, MinimumIntervalSeconds, MaximumIntervalSeconds);
            if (SetProperty(ref _updateIntervalSeconds, clamped) && !_isInitializingSettings)
            {
                SaveSettings();
            }
        }
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set
        {
            if (SetProperty(ref _minimizeToTray, value) && !_isInitializingSettings)
            {
                SaveSettings();
            }
        }
    }

    public StatusDisplayMode StatusDisplayMode
    {
        get => _statusDisplayMode;
        set
        {
            if (SetProperty(ref _statusDisplayMode, value) && !_isInitializingSettings)
            {
                OnPropertyChanged(nameof(DisplayedStatusText));
                SaveSettings();
            }
            else if (!_isInitializingSettings)
            {
                OnPropertyChanged(nameof(DisplayedStatusText));
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Logger.LogInfo("Disposing MainViewModel");

        // Unsubscribe from events
        if (_monitoringService != null)
        {
            _monitoringService.StreamStatusUpdated -= OnStreamStatusUpdated;
            _monitoringService.StatusMessageUpdated -= OnStatusMessageUpdated;
            _monitoringService.ErrorOccurred -= OnErrorOccurred;
            _monitoringService.MonitoringStarted -= OnMonitoringStarted;
            _monitoringService.MonitoringStopped -= OnMonitoringStopped;
        }

        _monitoringService?.Dispose();
        _statusChecker.Dispose();
        SaveSettings();
        
        Logger.LogInfo("MainViewModel disposed successfully");
    }

    private void LoadSettings()
    {
        _isInitializingSettings = true;
        try
        {
            var settings = _settingsService.Load();
            Username = settings.LastUsername;
            UpdateIntervalSeconds = settings.UpdateIntervalSeconds;
            MinimizeToTray = settings.MinimizeToTray;
            StatusDisplayMode = settings.StatusDisplayMode;
        }
        finally
        {
            _isInitializingSettings = false;
        }
    }

    private void SaveSettings()
    {
        if (_isInitializingSettings)
        {
            return;
        }

        var settings = new AppSettings
        {
            LastUsername = Username,
            UpdateIntervalSeconds = UpdateIntervalSeconds,
            MinimizeToTray = MinimizeToTray,
            StatusDisplayMode = StatusDisplayMode
        };
        _settingsService.Save(settings);
    }

    private void ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            _ = StopMonitoringAsync();
        }
        else
        {
            _ = StartMonitoringAsync();
        }
    }

    private bool CanToggleMonitoring()
    {
        return IsMonitoring || string.IsNullOrEmpty(UsernameValidationMessage);
    }

    private async Task StartMonitoringAsync()
    {
        if (IsMonitoring)
        {
            return;
        }

        ValidateUsername();
        if (!string.IsNullOrEmpty(UsernameValidationMessage))
        {
            Logger.LogWarning($"Cannot start monitoring - username validation failed: {UsernameValidationMessage}");
            return;
        }

        ErrorMessage = string.Empty;
        StatusMessage = "Starting monitoring...";

        try
        {
            Logger.LogInfo($"User requested to start monitoring for {Username}");
            await _monitoringService.StartMonitoringAsync(Username, UpdateIntervalSeconds).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to start monitoring", ex);
            ErrorMessage = $"Failed to start monitoring: {ex.Message}";
            StatusMessage = "Failed to start monitoring";
        }
    }

    private async Task StopMonitoringAsync()
    {
        if (!IsMonitoring)
        {
            return;
        }

        StatusMessage = "Stopping monitoring...";

        try
        {
            Logger.LogInfo("User requested to stop monitoring");
            await _monitoringService.StopMonitoringAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to stop monitoring", ex);
            ErrorMessage = $"Failed to stop monitoring: {ex.Message}";
            StatusMessage = "Failed to stop monitoring";
        }
    }

    private void OnStreamStatusUpdated(object? sender, global::KickStatusChecker.Models.StreamInfo? info)
    {
        if (info != null)
        {
            StreamTitle = info.Title;
            Category = info.Category;
            IsLive = info.IsLive;
            CurrentStatusText = info.IsLive ? "Online" : "Offline";
            ErrorMessage = string.Empty;
        }
        else
        {
            StreamTitle = string.Empty;
            Category = string.Empty;
            IsLive = false;
            CurrentStatusText = "Offline";
            ErrorMessage = "No stream data available";
        }

        _lastUpdated = DateTime.UtcNow;
        OnPropertyChanged(nameof(LastUpdatedDisplay));
        OnPropertyChanged(nameof(DisplayedStatusText));
    }

    private void OnStatusMessageUpdated(object? sender, string message)
    {
        StatusMessage = message;
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        ErrorMessage = error;
    }

    private void OnMonitoringStarted(object? sender, EventArgs e)
    {
        IsMonitoring = true;
    }

    private void OnMonitoringStopped(object? sender, EventArgs e)
    {
        IsMonitoring = false;
    }

    private void ValidateUsername()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            UsernameValidationMessage = "Please enter a Kick username.";
            return;
        }

        if (!UsernameRegex.IsMatch(Username))
        {
            UsernameValidationMessage = "Usernames may only contain letters, numbers, and underscores.";
            return;
        }

        UsernameValidationMessage = string.Empty;
    }

    private void UpdateStatusDisplay()
    {
        OnPropertyChanged(nameof(DisplayedStatusText));
        OnPropertyChanged(nameof(CurrentStatusText));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null)
        {
            return;
        }

        if (_syncContext != null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Post(_ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)), null);
        }
        else
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value) || propertyName is null)
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
