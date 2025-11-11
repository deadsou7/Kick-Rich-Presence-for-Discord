using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using KickStatusChecker.Wpf.Models;

namespace KickStatusChecker.Wpf.Services;

public class SettingsService
{
    private const int MinInterval = 10;
    private const int MaxInterval = 30;
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDirectory = Path.Combine(appData, "KickStatusChecker");
        Directory.CreateDirectory(settingsDirectory);
        _settingsPath = Path.Combine(settingsDirectory, "settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = true
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                    if (settings != null)
                    {
                        settings.UpdateIntervalSeconds = Math.Clamp(settings.UpdateIntervalSeconds, MinInterval, MaxInterval);
                        return settings;
                    }
                }
            }
        }
        catch
        {
            // Ignore and return default settings
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        if (settings is null)
        {
            return;
        }

        try
        {
            settings.UpdateIntervalSeconds = Math.Clamp(settings.UpdateIntervalSeconds, MinInterval, MaxInterval);
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Ignore write errors for now
        }
    }
}
