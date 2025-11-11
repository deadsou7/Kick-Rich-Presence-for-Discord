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

    private readonly KickStatusChecker.KickStatusChecker _statusChecker;
    private readonly SettingsService _settingsService;
    private readonly SynchronizationContext? _syncContext;
    private readonly RelayCommand _toggleMonitoringCommand;
    private readonly RelayCommand _exitCommand;
    private readonly RelayCommand _minimizeCommand;

    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;
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

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? RequestExit;
    public event EventHandler? RequestMinimize;

    public IReadOnlyList<StatusDisplayOption> StatusDisplayModes { get; }

    public MainViewModel()
    {
        _syncContext = SynchronizationContext.Current;
        _statusChecker = new KickStatusChecker.KickStatusChecker();
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

        LoadSettings();
        ValidateUsername();
        UpdateStatusDisplay();
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
        get => _isMonitoring;
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

        StopMonitoring();
        _statusChecker.Dispose();
        SaveSettings();
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
            StopMonitoring();
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
            return;
        }

        ErrorMessage = string.Empty;
        IsMonitoring = true;
        _monitoringCts = new CancellationTokenSource();
        var token = _monitoringCts.Token;

        // Kick off monitoring loop
        _monitoringTask = Task.Run(() => MonitorLoopAsync(token), token);

        await FetchStatusAsync(token).ConfigureAwait(false);
    }

    private async Task MonitorLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(UpdateIntervalSeconds), token).ConfigureAwait(false);
                await FetchStatusAsync(token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping monitoring
        }
    }

    private void StopMonitoring()
    {
        if (!IsMonitoring)
        {
            return;
        }

        try
        {
            _monitoringCts?.Cancel();
            _monitoringTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // ignored
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _monitoringCts?.Dispose();
            _monitoringCts = null;
            _monitoringTask = null;
            IsMonitoring = false;
        }
    }

    private async Task FetchStatusAsync(CancellationToken token)
    {
        try
        {
            var username = Username;
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            var info = await _statusChecker.GetStreamStatusAsync(username).ConfigureAwait(false);
            if (info == null)
            {
                UpdateStreamInfo(null, $"Channel '{username}' was not found or returned no data.");
            }
            else
            {
                UpdateStreamInfo(info, string.Empty);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException && token.IsCancellationRequested)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            UpdateStreamInfo(null, $"Unable to fetch status: {ex.Message}");
        }
    }

    private void UpdateStreamInfo(KickStatusChecker.Models.StreamInfo? info, string error)
    {
        if (info != null)
        {
            StreamTitle = info.Title;
            Category = info.Category;
            IsLive = info.IsLive;
            CurrentStatusText = info.IsLive ? "Online" : "Offline";
            ErrorMessage = error;
        }
        else
        {
            StreamTitle = string.Empty;
            Category = string.Empty;
            IsLive = false;
            CurrentStatusText = "Offline";
            ErrorMessage = error;
        }

        _lastUpdated = DateTime.UtcNow;
        OnPropertyChanged(nameof(LastUpdatedDisplay));
        OnPropertyChanged(nameof(DisplayedStatusText));
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
