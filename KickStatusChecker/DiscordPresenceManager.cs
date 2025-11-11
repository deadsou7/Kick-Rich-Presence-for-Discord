using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KickStatusChecker.Models;

namespace KickStatusChecker.Discord;

public sealed class DiscordPresenceManager : IAsyncDisposable
{
    public const ulong ApplicationId = 1437566854791958599UL;

    private readonly DiscordSocketClient _client;
    private readonly DiscordPresenceManagerOptions _options;
    private readonly Func<LogMessage, Task>? _logHandler;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly SemaphoreSlim _presenceLock = new(1, 1);
    private TaskCompletionSource<bool> _readySignal = CreateReadySignal();
    private StreamPresenceSnapshot? _lastSnapshot;
    private DateTimeOffset _lastUpdate;
    private string? _token;
    private bool _isDisposed;

    public DiscordPresenceManager(DiscordPresenceManagerOptions? options = null)
    {
        _options = options ?? new DiscordPresenceManagerOptions();
        _logHandler = _options.LogHandler;

        var config = _options.SocketConfig ?? CreateDefaultConfig();
        _client = new DiscordSocketClient(config);

        _client.Log += OnClientLogAsync;
        _client.Connected += OnConnectedAsync;
        _client.Disconnected += OnDisconnectedAsync;
        _client.Ready += OnReadyAsync;
        _client.Resumed += OnResumedAsync;
    }

    public DiscordSocketClient Client => _client;

    public async Task InitializeAsync(string botToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(botToken))
            throw new ArgumentException("Bot token must be provided.", nameof(botToken));

        ThrowIfDisposed();

        await ConnectInternalAsync(botToken, cancellationToken, isReconnect: false).ConfigureAwait(false);
    }

    public async Task UpdatePresenceAsync(StreamInfo streamInfo, CancellationToken cancellationToken = default)
    {
        if (streamInfo is null)
            throw new ArgumentNullException(nameof(streamInfo));

        ThrowIfDisposed();
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        var snapshot = StreamPresenceSnapshot.FromStream(streamInfo);

        if (ShouldSkipUpdate(snapshot))
            return;

        await _presenceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!streamInfo.IsLive)
            {
                var offlineMessage = _options.OfflineMessage ?? $"{streamInfo.Username} is offline";
                await SetOfflinePresenceInternalAsync(offlineMessage).ConfigureAwait(false);
                return;
            }

            var title = string.IsNullOrWhiteSpace(streamInfo.Title)
                ? $"{streamInfo.Username} is live on Kick"
                : streamInfo.Title.Trim();

            var category = string.IsNullOrWhiteSpace(streamInfo.Category)
                ? "Kick Streaming"
                : streamInfo.Category.Trim();

            var combined = $"{title} • {category}";
            if (combined.Length > 128)
                combined = combined[..128];

            var streamUrl = string.IsNullOrWhiteSpace(streamInfo.ChannelUrl)
                ? $"https://kick.com/{streamInfo.Username}"
                : streamInfo.ChannelUrl.Trim();

            await _client.SetStatusAsync(UserStatus.Online).ConfigureAwait(false);
            await _client.SetGameAsync(combined, streamUrl, ActivityType.Streaming).ConfigureAwait(false);

            _lastSnapshot = snapshot;
            _lastUpdate = DateTimeOffset.UtcNow;
        }
        finally
        {
            _presenceLock.Release();
        }
    }

    public async Task SetOfflinePresenceAsync(string? message = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        var snapshot = StreamPresenceSnapshot.Offline(message ?? _options.OfflineMessage);

        if (ShouldSkipUpdate(snapshot))
            return;

        await _presenceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SetOfflinePresenceInternalAsync(snapshot.OfflineMessage).ConfigureAwait(false);
        }
        finally
        {
            _presenceLock.Release();
        }
    }

    private async Task SetOfflinePresenceInternalAsync(string? offlineMessage)
    {
        var message = string.IsNullOrWhiteSpace(offlineMessage) ? "Kick stream offline" : offlineMessage!.Trim();

        if (string.IsNullOrWhiteSpace(message))
            await _client.SetGameAsync(null).ConfigureAwait(false);
        else
            await _client.SetGameAsync(message, null, ActivityType.Playing).ConfigureAwait(false);

        await _client.SetStatusAsync(UserStatus.Idle).ConfigureAwait(false);

        _lastSnapshot = StreamPresenceSnapshot.Offline(message);
        _lastUpdate = DateTimeOffset.UtcNow;
    }

    private async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        if (_client.ConnectionState == ConnectionState.Disconnected && _token is not null)
        {
            await ConnectInternalAsync(_token, cancellationToken, isReconnect: true).ConfigureAwait(false);
        }

        if (_readySignal.Task.IsCompleted)
            return;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var timeoutTask = Task.Delay(_options.ReadyTimeout, linkedCts.Token);

        var completed = await Task.WhenAny(_readySignal.Task, timeoutTask).ConfigureAwait(false);
        if (completed == timeoutTask)
            throw new TimeoutException("Timed out waiting for Discord client to become ready.");

        linkedCts.Cancel();
        await _readySignal.Task.ConfigureAwait(false);
    }

    private async Task ConnectInternalAsync(string botToken, CancellationToken cancellationToken, bool isReconnect)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(DiscordPresenceManager));

            _token = botToken;

            if (_client.LoginState != LoginState.LoggedIn)
            {
                await _client.LoginAsync(TokenType.Bot, botToken).ConfigureAwait(false);
            }

            if (_client.ConnectionState == ConnectionState.Disconnected)
            {
                await _client.StartAsync().ConfigureAwait(false);
            }

            await LogAsync(LogSeverity.Info, $"{(isReconnect ? "Reconnected" : "Connected")} to Discord (Application ID {ApplicationId}).").ConfigureAwait(false);
        }
        catch
        {
            _readySignal = CreateReadySignal();
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private bool ShouldSkipUpdate(StreamPresenceSnapshot snapshot)
    {
        if (_lastSnapshot is null)
            return false;

        if (!_lastSnapshot.Value.Equals(snapshot))
            return false;

        if (_options.MinUpdateInterval <= TimeSpan.Zero)
            return true;

        return DateTimeOffset.UtcNow - _lastUpdate < _options.MinUpdateInterval;
    }

    private Task OnClientLogAsync(LogMessage message) => LogAsync(message);

    private Task OnConnectedAsync()
    {
        _readySignal.TrySetResult(true);
        return LogAsync(LogSeverity.Info, "Connected to Discord gateway.");
    }

    private Task OnReadyAsync()
    {
        _readySignal.TrySetResult(true);
        var username = _client.CurrentUser?.ToString() ?? "unknown user";
        return LogAsync(LogSeverity.Info, $"Discord client ready as {username}.");
    }

    private Task OnResumedAsync(int shardId)
    {
        _readySignal.TrySetResult(true);
        return LogAsync(LogSeverity.Info, $"Discord session resumed (shard {shardId}).");
    }

    private Task OnDisconnectedAsync(Exception? exception)
    {
        _readySignal = CreateReadySignal();

        var logTask = exception is null
            ? LogAsync(LogSeverity.Warning, "Discord connection closed.")
            : LogAsync(LogSeverity.Warning, exception, "Discord connection closed unexpectedly.");

        if (_options.AutoReconnect && !_isDisposed && _token is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_options.ReconnectDelay).ConfigureAwait(false);
                    if (_isDisposed)
                        return;

                    await ConnectInternalAsync(_token, CancellationToken.None, isReconnect: true).ConfigureAwait(false);
                }
                catch (Exception reconnectEx)
                {
                    await LogAsync(LogSeverity.Error, reconnectEx, "Automatic Discord reconnect failed.").ConfigureAwait(false);
                }
            });
        }

        return logTask;
    }

    private Task LogAsync(LogSeverity severity, string message)
        => LogAsync(new LogMessage(severity, nameof(DiscordPresenceManager), message));

    private Task LogAsync(LogSeverity severity, Exception exception, string message)
        => LogAsync(new LogMessage(severity, nameof(DiscordPresenceManager), message, exception));

    private Task LogAsync(LogMessage message)
        => _logHandler?.Invoke(message) ?? Task.CompletedTask;

    private static TaskCompletionSource<bool> CreateReadySignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static DiscordSocketConfig CreateDefaultConfig()
        => new()
        {
            LogLevel = LogSeverity.Warning,
            GatewayIntents = GatewayIntents.Guilds,
            AlwaysDownloadUsers = false,
            MessageCacheSize = 0,
            DefaultRetryMode = RetryMode.AlwaysRetry
        };

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(DiscordPresenceManager));
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _client.Log -= OnClientLogAsync;
        _client.Connected -= OnConnectedAsync;
        _client.Disconnected -= OnDisconnectedAsync;
        _client.Ready -= OnReadyAsync;
        _client.Resumed -= OnResumedAsync;

        try
        {
            if (_client.LoginState == LoginState.LoggedIn)
            {
                await _client.LogoutAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await LogAsync(LogSeverity.Warning, ex, "Failed to log out Discord client cleanly.").ConfigureAwait(false);
        }

        try
        {
            if (_client.ConnectionState != ConnectionState.Disconnected)
            {
                await _client.StopAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await LogAsync(LogSeverity.Warning, ex, "Failed to stop Discord client cleanly.").ConfigureAwait(false);
        }

        _client.Dispose();
        _connectionLock.Dispose();
        _presenceLock.Dispose();
    }

    private readonly record struct StreamPresenceSnapshot(
        bool IsLive,
        string? Title,
        string? Category,
        string? Url,
        string? OfflineMessage)
    {
        public static StreamPresenceSnapshot FromStream(StreamInfo info)
            => new(info.IsLive,
                info.Title?.Trim(),
                info.Category?.Trim(),
                string.IsNullOrWhiteSpace(info.ChannelUrl) ? null : info.ChannelUrl.Trim(),
                null);

        public static StreamPresenceSnapshot Offline(string? message)
            => new(false, null, null, null, string.IsNullOrWhiteSpace(message) ? null : message.Trim());
    }
}

public sealed class DiscordPresenceManagerOptions
{
    public DiscordSocketConfig? SocketConfig { get; init; }
    public Func<LogMessage, Task>? LogHandler { get; init; }
    public bool AutoReconnect { get; init; } = true;
    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan ReadyTimeout { get; init; } = TimeSpan.FromSeconds(20);
    public TimeSpan MinUpdateInterval { get; init; } = TimeSpan.FromSeconds(10);
    public string? OfflineMessage { get; init; }
}
