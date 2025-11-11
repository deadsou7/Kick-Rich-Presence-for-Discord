using System;
using System.Threading;
using System.Threading.Tasks;
using KickStatusChecker.Wpf.Models;

namespace KickStatusChecker.Wpf.Services;

public class MonitoringService : IDisposable
{
    private readonly global::KickStatusChecker.KickStatusChecker _kickStatusChecker;
    private readonly DiscordPresenceManager _discordPresenceManager;
    private readonly SynchronizationContext? _syncContext;

    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;
    private bool _disposed;
    private global::KickStatusChecker.Models.StreamInfo? _lastStreamInfo;
    private string _lastUsername = string.Empty;

    public event EventHandler<global::KickStatusChecker.Models.StreamInfo?>? StreamStatusUpdated;
    public event EventHandler<string>? StatusMessageUpdated;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? MonitoringStarted;
    public event EventHandler? MonitoringStopped;

    public bool IsMonitoring { get; private set; }

    public MonitoringService(
        global::KickStatusChecker.KickStatusChecker kickStatusChecker,
        DiscordPresenceManager discordPresenceManager,
        SynchronizationContext? syncContext = null)
    {
        _kickStatusChecker = kickStatusChecker ?? throw new ArgumentNullException(nameof(kickStatusChecker));
        _discordPresenceManager = discordPresenceManager ?? throw new ArgumentNullException(nameof(discordPresenceManager));
        _syncContext = syncContext;
    }

    public async Task StartMonitoringAsync(string username, int intervalSeconds, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MonitoringService));

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        if (IsMonitoring)
            await StopMonitoringAsync().ConfigureAwait(false);

        Logger.LogInfo($"Starting monitoring for {username} with {intervalSeconds}s interval");

        _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _monitoringCts.Token;

        IsMonitoring = true;
        _lastUsername = username.Trim().ToLowerInvariant();
        
        // Clear previous state when starting with a new username
        if (_lastStreamInfo?.Username.Equals(_lastUsername, StringComparison.OrdinalIgnoreCase) != true)
        {
            Logger.LogInfo($"New username detected, clearing previous state");
            _lastStreamInfo = null;
            await _discordPresenceManager.ClearPresenceAsync(token).ConfigureAwait(false);
        }

        OnMonitoringStarted();
        OnStatusMessageUpdated($"Started monitoring {_lastUsername} every {intervalSeconds} seconds");

        // Start monitoring loop
        _monitoringTask = Task.Run(() => MonitoringLoopAsync(_lastUsername, intervalSeconds, token), token);

        // Do initial check immediately
        await CheckAndUpdateStatusAsync(_lastUsername, token).ConfigureAwait(false);
    }

    public async Task StopMonitoringAsync()
    {
        if (!IsMonitoring)
            return;

        Logger.LogInfo("Stopping monitoring");

        try
        {
            _monitoringCts?.Cancel();
            
            if (_monitoringTask != null)
            {
                try
                {
                    await _monitoringTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected during cancellation
                }
            }

            await _discordPresenceManager.ClearPresenceAsync().ConfigureAwait(false);
        }
        finally
        {
            _monitoringCts?.Dispose();
            _monitoringCts = null;
            _monitoringTask = null;
            IsMonitoring = false;
            
            OnMonitoringStopped();
            OnStatusMessageUpdated("Monitoring stopped");
            Logger.LogInfo("Monitoring stopped successfully");
        }
    }

    private async Task MonitoringLoopAsync(string username, int intervalSeconds, CancellationToken token)
    {
        Logger.LogInfo($"Starting monitoring loop for {username}");
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                Logger.LogDebug($"Waiting {intervalSeconds} seconds before next check");
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), token).ConfigureAwait(false);
                await CheckAndUpdateStatusAsync(username, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogInfo("Monitoring loop cancelled");
            // Expected when stopping monitoring
        }
        catch (Exception ex)
        {
            Logger.LogError("Monitoring loop error", ex);
            OnErrorOccurred($"Monitoring loop error: {ex.Message}");
        }
    }

    private async Task CheckAndUpdateStatusAsync(string username, CancellationToken token)
    {
        Logger.LogDebug($"Checking stream status for {username}");
        
        try
        {
            var streamInfo = await _kickStatusChecker.GetStreamStatusAsync(username, maxRetries: 3).ConfigureAwait(false);
            
            if (streamInfo == null)
            {
                Logger.LogWarning($"No stream data returned for {username}");
                
                // Channel not found or no data
                var offlineInfo = new global::KickStatusChecker.Models.StreamInfo
                {
                    Username = username,
                    Title = string.Empty,
                    Category = string.Empty,
                    IsLive = false,
                    ChannelUrl = $"https://kick.com/{username}",
                    FetchedAt = DateTime.UtcNow
                };
                
                await ProcessStreamInfoAsync(offlineInfo, token).ConfigureAwait(false);
            }
            else
            {
                Logger.LogInfo($"Stream status for {username}: {(streamInfo.IsLive ? "ONLINE" : "OFFLINE")} - {streamInfo.Title}");
                await ProcessStreamInfoAsync(streamInfo, token).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            Logger.LogError($"Failed to check stream status for {username}", ex);
            OnErrorOccurred($"Failed to check stream status: {ex.Message}");
            
            // Create offline info on error
            var errorInfo = new global::KickStatusChecker.Models.StreamInfo
            {
                Username = username,
                Title = string.Empty,
                Category = string.Empty,
                IsLive = false,
                ChannelUrl = $"https://kick.com/{username}",
                FetchedAt = DateTime.UtcNow
            };
            
            await ProcessStreamInfoAsync(errorInfo, token).ConfigureAwait(false);
        }
    }

    private async Task ProcessStreamInfoAsync(global::KickStatusChecker.Models.StreamInfo streamInfo, CancellationToken token)
    {
        // Detect changes
        var hasChanged = HasStreamStatusChanged(streamInfo);
        
        if (hasChanged)
        {
            Logger.LogInfo($"Stream status changed for {streamInfo.Username}: {(streamInfo.IsLive ? "ONLINE" : "OFFLINE")}");
            
            // Update Discord presence only when status changes
            var discordSuccess = await _discordPresenceManager.UpdatePresenceAsync(
                streamInfo.Username, 
                streamInfo.IsLive, 
                streamInfo.Title, 
                streamInfo.Category, 
                token).ConfigureAwait(false);

            if (!discordSuccess)
            {
                Logger.LogError("Failed to update Discord presence");
                OnErrorOccurred("Failed to update Discord presence");
            }
        }
        else
        {
            Logger.LogDebug($"No status change for {streamInfo.Username}");
        }

        _lastStreamInfo = streamInfo;
        
        // Update GUI regardless of changes (to refresh timestamps)
        OnStreamStatusUpdated(streamInfo);
        
        if (hasChanged)
        {
            OnStatusMessageUpdated($"Status updated: {(streamInfo.IsLive ? "Online" : "Offline")}");
        }
    }

    private bool HasStreamStatusChanged(global::KickStatusChecker.Models.StreamInfo currentInfo)
    {
        if (_lastStreamInfo == null)
        {
            Logger.LogDebug($"First status check for {currentInfo.Username}");
            return true;
        }

        // Check for relevant changes that would require Discord presence update
        var liveChanged = _lastStreamInfo.IsLive != currentInfo.IsLive;
        var titleChanged = !string.Equals(_lastStreamInfo.Title, currentInfo.Title, StringComparison.OrdinalIgnoreCase);
        var categoryChanged = !string.Equals(_lastStreamInfo.Category, currentInfo.Category, StringComparison.OrdinalIgnoreCase);

        if (liveChanged || titleChanged || categoryChanged)
        {
            Logger.LogDebug($"Status change detected for {currentInfo.Username}: Live={liveChanged}, Title={titleChanged}, Category={categoryChanged}");
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Logger.LogInfo("Disposing MonitoringService");

        try
        {
            StopMonitoringAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore errors during disposal
        }

        _discordPresenceManager?.Dispose();
    }

    private void OnStreamStatusUpdated(global::KickStatusChecker.Models.StreamInfo? streamInfo)
    {
        if (_syncContext != null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Post(_ => StreamStatusUpdated?.Invoke(this, streamInfo), null);
        }
        else
        {
            StreamStatusUpdated?.Invoke(this, streamInfo);
        }
    }

    private void OnStatusMessageUpdated(string message)
    {
        if (_syncContext != null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Post(_ => StatusMessageUpdated?.Invoke(this, message), null);
        }
        else
        {
            StatusMessageUpdated?.Invoke(this, message);
        }
    }

    private void OnErrorOccurred(string error)
    {
        if (_syncContext != null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Post(_ => ErrorOccurred?.Invoke(this, error), null);
        }
        else
        {
            ErrorOccurred?.Invoke(this, error);
        }
    }

    private void OnMonitoringStarted()
    {
        if (_syncContext != null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Post(_ => MonitoringStarted?.Invoke(this, EventArgs.Empty), null);
        }
        else
        {
            MonitoringStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnMonitoringStopped()
    {
        if (_syncContext != null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Post(_ => MonitoringStopped?.Invoke(this, EventArgs.Empty), null);
        }
        else
        {
            MonitoringStopped?.Invoke(this, EventArgs.Empty);
        }
    }
}