using System;
using System.IO;
using System.Threading;

namespace KickStatusChecker.Wpf.Services;

public static class Logger
{
    private static readonly string _logPath;
    private static readonly object _lock = new();

    static Logger()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDirectory = Path.Combine(appData, "KickStatusChecker", "Logs");
        Directory.CreateDirectory(logDirectory);
        
        var fileName = $"monitoring_{DateTime.Now:yyyyMMdd}.log";
        _logPath = Path.Combine(logDirectory, fileName);
    }

    public static void LogInfo(string message)
    {
        Log("INFO", message);
    }

    public static void LogError(string message, Exception? exception = null)
    {
        var fullMessage = exception != null ? $"{message} - {exception}" : message;
        Log("ERROR", fullMessage);
    }

    public static void LogWarning(string message)
    {
        Log("WARN", message);
    }

    public static void LogDebug(string message)
    {
        Log("DEBUG", message);
    }

    private static void Log(string level, string message)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var logEntry = $"[{timestamp}] [{level}] [T{threadId}] {message}{Environment.NewLine}";

            lock (_lock)
            {
                File.AppendAllText(_logPath, logEntry);
            }
        }
        catch
        {
            // Ignore logging errors to prevent infinite loops
        }
    }
}