using System;
using System.Threading;
using System.Threading.Tasks;

namespace KickStatusChecker.Wpf.Services;

public class DiscordPresenceManager : IDisposable
{
    private bool _disposed;
    private string _lastPresence = string.Empty;

    public event EventHandler<string>? PresenceUpdated;

    public async Task<bool> UpdatePresenceAsync(string username, bool isLive, string title = "", string category = "", CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return false;

        try
        {
            var presenceText = BuildPresenceText(username, isLive, title, category);
            
            // Only update if the presence actually changed
            if (presenceText == _lastPresence)
            {
                Logger.LogDebug($"Discord presence unchanged for {username}: {presenceText}");
                return true;
            }

            Logger.LogInfo($"Updating Discord presence for {username}: {presenceText}");

            // For now, we'll simulate Discord presence updates
            // In a real implementation, this would use Discord Rich Presence API
            await Task.Delay(10, cancellationToken);
            
            _lastPresence = presenceText;
            PresenceUpdated?.Invoke(this, presenceText);
            
            Logger.LogInfo($"Discord presence updated successfully for {username}");
            return true;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Discord presence update cancelled");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to update Discord presence", ex);
            return false;
        }
    }

    public async Task ClearPresenceAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;

        try
        {
            Logger.LogInfo("Clearing Discord presence");
            await Task.Delay(10, cancellationToken);
            _lastPresence = string.Empty;
            PresenceUpdated?.Invoke(this, "Presence cleared");
            Logger.LogInfo("Discord presence cleared successfully");
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Discord presence clear cancelled");
            // Expected during cancellation
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to clear Discord presence", ex);
            // Ignore errors during cleanup
        }
    }

    private string BuildPresenceText(string username, bool isLive, string title, string category)
    {
        if (isLive)
        {
            var details = string.IsNullOrWhiteSpace(title) ? "Live on Kick" : title;
            var state = string.IsNullOrWhiteSpace(category) ? "Streaming" : category;
            return $"🔴 {username} - {details} | {state}";
        }
        else
        {
            return $"⚫ {username} - Offline";
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Logger.LogInfo("Disposing DiscordPresenceManager");
        _ = ClearPresenceAsync(CancellationToken.None);
    }
}