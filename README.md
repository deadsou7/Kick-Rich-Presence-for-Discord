# Kick Stream Status Checker

A C# library for monitoring Kick stream status and retrieving stream information.

## Features

- ✅ Check if a Kick streamer is live or offline
- ✅ Retrieve stream title and category/game
- ✅ Get channel URL and username information
- ✅ Built-in caching (12 seconds) to avoid excessive API calls
- ✅ Retry logic for handling network failures
- ✅ Graceful error handling
- ✅ Async/await support
- ✅ Easy to use and testable

## Installation

Add the library to your project:

```bash
dotnet add package KickStatusChecker
```

Or reference the project directly:

```xml
<ProjectReference Include="..\KickStatusChecker\KickStatusChecker.csproj" />
```

## Quick Start

```csharp
using KickStatusChecker;
using KickStatusChecker.Models;

// Create an instance of the checker
using var checker = new KickStatusChecker();

// Get stream status for a username
var streamInfo = await checker.GetStreamStatusAsync("xqc");

if (streamInfo != null)
{
    Console.WriteLine($"Status: {(streamInfo.IsLive ? "🔴 LIVE" : "⚫ OFFLINE")}");
    Console.WriteLine($"Title: {streamInfo.Title}");
    Console.WriteLine($"Category: {streamInfo.Category}");
    Console.WriteLine($"Channel: {streamInfo.ChannelUrl}");
}
else
{
    Console.WriteLine("User not found or stream is offline");
}
```

## API Reference

### KickStatusChecker Class

#### Constructor
```csharp
public KickStatusChecker()
```
Creates a new instance of the KickStatusChecker with default settings.

#### Methods

##### GetStreamStatusAsync
```csharp
public async Task<StreamInfo?> GetStreamStatusAsync(string username, int maxRetries = 3)
```
Retrieves stream information for the specified Kick username.

**Parameters:**
- `username`: The Kick username to check (case-insensitive)
- `maxRetries`: Maximum number of retry attempts for network failures (default: 3)

**Returns:**
- `StreamInfo?`: Stream information if found, null if user doesn't exist or stream is offline

**Exceptions:**
- `ArgumentException`: Thrown when username is null, empty, or whitespace

##### ClearCache
```csharp
public void ClearCache()
```
Clears the internal cache, forcing the next request to fetch fresh data.

##### Dispose
```csharp
public void Dispose()
```
Disposes the underlying HttpClient and other resources.

### StreamInfo Class

Represents information about a Kick stream.

#### Properties

- `string Title`: Current stream title
- `string Category`: Current category/game being played
- `bool IsLive`: Whether the stream is currently live
- `string ChannelUrl`: URL to the Kick channel (https://kick.com/{username})
- `string Username`: The channel username
- `DateTime FetchedAt`: When the information was fetched (UTC)

## Usage Examples

### Basic Usage

```csharp
using var checker = new KickStatusChecker();

var info = await checker.GetStreamStatusAsync("destiny");
if (info != null)
{
    Console.WriteLine($"{info.Username} is {(info.IsLive ? "LIVE" : "OFFLINE")}");
    if (info.IsLive)
    {
        Console.WriteLine($"Streaming: {info.Title}");
        Console.WriteLine($"Category: {info.Category}");
    }
}
```

### Error Handling

```csharp
try
{
    using var checker = new KickStatusChecker();
    var info = await checker.GetStreamStatusAsync("username");
    
    if (info != null)
    {
        // Process stream info
    }
    else
    {
        Console.WriteLine("User not found or offline");
    }
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid username: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Failed to fetch stream info: {ex.Message}");
}
```

### Batch Processing

```csharp
using var checker = new KickStatusChecker();
var usernames = new[] { "xqc", "destiny", "trainwreck" };

foreach (var username in usernames)
{
    try
    {
        var info = await checker.GetStreamStatusAsync(username);
        Console.WriteLine($"{username}: {(info?.IsLive == true ? "LIVE" : "OFFLINE")}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error checking {username}: {ex.Message}");
    }
}
```

### Cache Management

```csharp
using var checker = new KickStatusChecker();

// First call - fetches from API
var info1 = await checker.GetStreamStatusAsync("xqc");

// Second call within 12 seconds - uses cache
var info2 = await checker.GetStreamStatusAsync("xqc");

// Clear cache to force fresh fetch
checker.ClearCache();
var info3 = await checker.GetStreamStatusAsync("xqc");
```

## Implementation Details

### Caching
The library implements a 12-second cache to avoid excessive API calls to Kick. Cached results are returned immediately for repeated requests within this window.

### Retry Logic
Network requests are automatically retried with exponential backoff when failures occur:
- Attempt 1: Immediate
- Attempt 2: Wait 2 seconds
- Attempt 3: Wait 4 seconds

### Data Sources
The library fetches data by:
1. Making HTTP requests to `https://kick.com/{username}`
2. Parsing JSON-LD structured data from the page
3. Falling back to HTML parsing if JSON data is not available
4. Extracting stream title, category, and live status

### Error Handling
- **404 Not Found**: Returns null (user doesn't exist)
- **Network Errors**: Retries with exponential backoff
- **Parse Errors**: Falls back to alternative parsing methods
- **Invalid Input**: Throws ArgumentException

## Dependencies

- .NET 8.0 or later
- HtmlAgilityPack (for HTML parsing)
- System.Text.Json (for JSON parsing)

## Building and Testing

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run the demo application
dotnet run --project KickStatusChecker.Demo
```

## License

This project is open source. Please refer to the LICENSE file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## Support

For issues and questions, please use the GitHub issue tracker.