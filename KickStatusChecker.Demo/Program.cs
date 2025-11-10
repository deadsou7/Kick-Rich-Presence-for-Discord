using System;
using System.Threading.Tasks;
using KickStatusChecker;
using KickStatusChecker.Models;

namespace KickStatusChecker.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Kick Stream Status Checker Demo");
        Console.WriteLine("================================");
        
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
                }
                else
                {
                    Console.WriteLine("User not found or stream is offline");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking {username}: {ex.Message}");
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
        
        Console.WriteLine("\nDemo completed. Press any key to exit...");
        Console.ReadKey();
    }
}