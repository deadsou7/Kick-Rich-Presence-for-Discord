namespace KickStatusChecker.Wpf.Models;

public class AppSettings
{
    public string LastUsername { get; set; } = string.Empty;
    public int UpdateIntervalSeconds { get; set; } = 15;
    public bool MinimizeToTray { get; set; } = true;
    public StatusDisplayMode StatusDisplayMode { get; set; } = StatusDisplayMode.Actual;
}
