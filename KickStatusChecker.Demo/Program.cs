using System;
using System.Threading.Tasks;
using KickStatusChecker;
using KickStatusChecker.Discord;
using KickStatusChecker.Models;

namespace KickStatusChecker.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Kick Stream Status Checker Demo");
        Console.WriteLine("================================");

        var discordToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        DiscordPresenceManager? presenceManager = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(discordToken))
            {
                presenceManager = new DiscordPresenceManager(new DiscordPresenceManagerOptions
                {
                    LogHandler = message =>
                    {
                        Console.WriteLine($"[Discord] {message.Severity}: {message.Message}");
                        if (message.Exception is not null)
                        {
                            Console.WriteLine(message.Exception);
                        }

                        return Task.CompletedTask;
                    },
                    MinUpdateInterval = TimeSpan.FromSeconds(20),
                    OfflineMessage = "Kick stream offline"
                });

                Console.WriteLine("\nConnecting to Discord Rich Presence...");
                await presenceManager.InitializeAsync(discordToken);
                Console.WriteLine("Discord Rich Presence connected.");
            }
            else
            {
                Console.WriteLine("\nSet the DISCORD_BOT_TOKEN environment variable to enable Discord Rich Presence updates in this demo.");
            }

            using var checker = new KickStatusChecker();

            // Example usernames to test
            string[] testUsernames = { "xqc", "destiny", "nonexistentuser12345" };

            foreach (var username in testUsernames)
            {
                Console.WriteLine($"\nChecking status for: {username}");
                Console.WriteLine("----------------------------------------");

                try
                {
                    var streamInfo = await checker.GetStreamStatusAsync(username);

                    if (streamInfo != null)
                    {
                        Console.WriteLine($"Username: {streamInfo.Username}");
                        Console.WriteLine($"Channel URL: {streamInfo.ChannelUrl}");
                        Console.WriteLine($"Status: {(streamInfo.IsLive ? "🔴 LIVE" : "⚫ OFFLINE")}");
                        Console.WriteLine($"Title: {streamInfo.Title}");
                        Console.WriteLine($"Category: {streamInfo.Category}");
                        Console.WriteLine($"Fetched at: {streamInfo.FetchedAt:yyyy-MM-dd HH:mm:ss} UTC");

                        if (presenceManager != null)
                        {
                            if (streamInfo.IsLive)
                            {
                                await presenceManager.UpdatePresenceAsync(streamInfo);
                            }
                            else
                            {
                                await presenceManager.SetOfflinePresenceAsync($"{streamInfo.Username} is offline");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("User not found or stream is offline");
                        if (presenceManager != null)
                        {
                            await presenceManager.SetOfflinePresenceAsync($"{username} is offline");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking {username}: {ex.Message}");
                    if (presenceManager != null)
                    {
                        await presenceManager.SetOfflinePresenceAsync($"Error checking {username}");
                    }
                }

                // Add a small delay between requests
                await Task.Delay(1000);
            }

            // Demonstrate caching
            Console.WriteLine("\n\nTesting cache behavior...");
            Console.WriteLine("Checking xqc again (should use cache):");

            var startTime = DateTime.UtcNow;
            var cachedResult = await checker.GetStreamStatusAsync("xqc");
            var endTime = DateTime.UtcNow;

            Console.WriteLine($"Request completed in {(endTime - startTime).TotalMilliseconds:F0}ms");
            Console.WriteLine($"Cache hit: {((endTime - startTime).TotalMilliseconds < 100 ? "Yes" : "No")}");

            // Clear cache and test again
            Console.WriteLine("\nClearing cache and checking again...");
            checker.ClearCache();

            startTime = DateTime.UtcNow;
            var freshResult = await checker.GetStreamStatusAsync("xqc");
            endTime = DateTime.UtcNow;

            Console.WriteLine($"Request completed in {(endTime - startTime).TotalMilliseconds:F0}ms");

            if (presenceManager != null)
            {
                if (freshResult != null && freshResult.IsLive)
                {
                    await presenceManager.UpdatePresenceAsync(freshResult);
                }
                else
                {
                    await presenceManager.SetOfflinePresenceAsync();
                }
            }

            Console.WriteLine("\nDemo completed. Press any key to exit...");
            Console.ReadKey();
        }
        finally
        {
            if (presenceManager is not null)
            {
                await presenceManager.DisposeAsync();
            }
        }
    }
}
