using System;
using System.Threading.Tasks;
using KickStatusChecker;
using KickStatusChecker.Models;

namespace KickStatusChecker.Example;

class Program
{
    static async Task Main(string[] args)
    {
        // Simple example of using the Kick Stream Status Checker
        using var checker = new KickStatusChecker();
        
        // Check a single streamer
        var streamInfo = await checker.GetStreamStatusAsync("xqc");
        
        if (streamInfo != null)
        {
            Console.WriteLine($"Username: {streamInfo.Username}");
            Console.WriteLine($"Status: {(streamInfo.IsLive ? "🔴 LIVE" : "⚫ OFFLINE")}");
            Console.WriteLine($"Title: {streamInfo.Title}");
            Console.WriteLine($"Category: {streamInfo.Category}");
            Console.WriteLine($"Channel: {streamInfo.ChannelUrl}");
        }
        
        // Example of checking multiple streamers
        string[] streamers = { "xqc", "destiny", "trainwreck" };
        
        foreach (var streamer in streamers)
        {
            var info = await checker.GetStreamStatusAsync(streamer);
            Console.WriteLine($"{streamer}: {(info?.IsLive == true ? "LIVE" : "OFFLINE")}");
        }
    }
}