namespace KickStatusChecker.Wpf.Models;

public class DiscordConfiguration
{
    public string ApplicationId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = false;
    
    // For future Discord Rich Presence integration
    // These would be obtained from Discord Developer Portal
    // https://discord.com/developers/applications
}