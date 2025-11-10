using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using HtmlAgilityPack;
using KickStatusChecker.Models;

namespace KickStatusChecker;

public class KickStatusChecker : IDisposable
{
    private readonly HttpClient _httpClient;
    private StreamInfo? _cachedInfo;
    private DateTime _lastFetchTime = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(12);

    public KickStatusChecker()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", 
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<StreamInfo?> GetStreamStatusAsync(string username, int maxRetries = 3)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        username = username.Trim().ToLowerInvariant();

        // Check cache first
        if (_cachedInfo != null && 
            _cachedInfo.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
            DateTime.UtcNow - _lastFetchTime < _cacheDuration)
        {
            return _cachedInfo;
        }

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var streamInfo = await FetchStreamInfoAsync(username);
                if (streamInfo != null)
                {
                    _cachedInfo = streamInfo;
                    _lastFetchTime = DateTime.UtcNow;
                    return streamInfo;
                }
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
            catch (Exception) when (attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
            }
        }

        // Return cached info if available, even if expired
        return _cachedInfo?.Username.Equals(username, StringComparison.OrdinalIgnoreCase) == true 
            ? _cachedInfo 
            : null!;
    }

    private async Task<StreamInfo?> FetchStreamInfoAsync(string username)
    {
        var channelUrl = $"https://kick.com/{username}";
        
        try
        {
            var response = await _httpClient.GetAsync(channelUrl);
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // Handle 403 Forbidden - likely anti-bot protection
                // Return a default offline state
                return new StreamInfo
                {
                    Username = username,
                    Title = string.Empty,
                    Category = string.Empty,
                    IsLive = false,
                    ChannelUrl = channelUrl,
                    FetchedAt = DateTime.UtcNow
                };
            }

            response.EnsureSuccessStatusCode();
            var htmlContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Look for JSON data in script tags
            var scriptTags = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json' or contains(text(), '__NUXT__') or contains(text(), 'window.__NUXT__')]");
            
            foreach (var script in scriptTags ?? Enumerable.Empty<HtmlNode>())
            {
                var scriptContent = script.InnerText;
                if (string.IsNullOrWhiteSpace(scriptContent))
                    continue;

                try
                {
                    // Try to parse as JSON-LD first
                    if (scriptContent.TrimStart().StartsWith("{"))
                    {
                        var jsonDoc = JsonDocument.Parse(scriptContent);
                        if (ExtractFromJsonLd(jsonDoc, username, out var streamInfo))
                        {
                            return streamInfo;
                        }
                    }
                }
                catch
                {
                    // Continue to next script if parsing fails
                }
            }

            // Fallback to HTML parsing
            return ExtractFromHtml(doc, username);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to fetch stream info for {username}", ex);
        }
    }

    private bool ExtractFromJsonLd(JsonDocument jsonDoc, string username, out StreamInfo streamInfo)
    {
        streamInfo = null!;
        var root = jsonDoc.RootElement;

        // Look for broadcast data in various possible structures
        if (root.ValueKind == JsonValueKind.Object)
        {
            // Check for direct broadcast data
            if (root.TryGetProperty("broadcast", out var broadcastElement) ||
                root.TryGetProperty("livestream", out broadcastElement))
            {
                if (broadcastElement.ValueKind == JsonValueKind.Object)
                {
                    streamInfo = CreateStreamInfoFromJson(broadcastElement, username);
                    return true;
                }
            }

            // Check nested structures
            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    if (property.Value.TryGetProperty("broadcast", out broadcastElement) ||
                        property.Value.TryGetProperty("livestream", out broadcastElement))
                    {
                        if (broadcastElement.ValueKind == JsonValueKind.Object)
                        {
                            streamInfo = CreateStreamInfoFromJson(broadcastElement, username);
                            return true;
                        }
                    }
                }
                else if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object &&
                            (item.TryGetProperty("broadcast", out broadcastElement) ||
                             item.TryGetProperty("livestream", out broadcastElement)))
                        {
                            if (broadcastElement.ValueKind == JsonValueKind.Object)
                            {
                                streamInfo = CreateStreamInfoFromJson(broadcastElement, username);
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    private StreamInfo CreateStreamInfoFromJson(JsonElement broadcastElement, string username)
    {
        var title = broadcastElement.TryGetProperty("title", out var titleElement) 
            ? titleElement.GetString() ?? string.Empty 
            : string.Empty;

        var category = broadcastElement.TryGetProperty("category", out var categoryElement) &&
                       categoryElement.ValueKind == JsonValueKind.Object &&
                       categoryElement.TryGetProperty("name", out var categoryNameElement)
            ? categoryNameElement.GetString() ?? string.Empty
            : string.Empty;

        return new StreamInfo
        {
            Username = username,
            Title = title,
            Category = category,
            IsLive = !string.IsNullOrWhiteSpace(title),
            ChannelUrl = $"https://kick.com/{username}",
            FetchedAt = DateTime.UtcNow
        };
    }

    private StreamInfo? ExtractFromHtml(HtmlDocument doc, string username)
    {
        var titleNode = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'stream-title') or contains(@class, 'title')]") ??
                        doc.DocumentNode.SelectSingleNode("//title");
        
        var categoryNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'category') or contains(@class, 'game')]") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'category') or contains(@class, 'game')]");

        var title = titleNode?.InnerText.Trim() ?? string.Empty;
        var category = categoryNode?.InnerText.Trim() ?? string.Empty;

        // Check if the stream appears to be live based on common indicators
        var isLive = !string.IsNullOrWhiteSpace(title) && 
                     doc.DocumentNode.InnerHtml.Contains("live", StringComparison.OrdinalIgnoreCase) &&
                     !doc.DocumentNode.InnerHtml.Contains("offline", StringComparison.OrdinalIgnoreCase);

        return new StreamInfo
        {
            Username = username,
            Title = title,
            Category = category,
            IsLive = isLive,
            ChannelUrl = $"https://kick.com/{username}",
            FetchedAt = DateTime.UtcNow
        };
    }

    public void ClearCache()
    {
        _cachedInfo = null;
        _lastFetchTime = DateTime.MinValue;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}