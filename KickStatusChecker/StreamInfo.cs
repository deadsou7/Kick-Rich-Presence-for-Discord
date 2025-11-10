namespace KickStatusChecker.Models;

public class StreamInfo
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsLive { get; set; }
    public string ChannelUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime FetchedAt { get; set; }
}