// Program.cs
using System;
using System.Threading.Tasks;
using WebScrapingApp; // Your namespace

public class Program
{
    // Configuration
    private const string UrlsFilePath = @".\resource\tsescorts_city_state_urls.csv"; // Ensure this path and file exist
    private const string OutputFilePath = @".\user_profile_urls.txt";
    private const int MaxConcurrentRequests = 25;  // Adjust based on your needs and server politeness
    private const int RequestDelayMilliseconds = 300; // Delay between individual requests

    static async Task Main(string[] args)
    {
        Console.WriteLine("Web Scraping Process Started...");
        Console.WriteLine($"--------------------------------");
        Console.WriteLine($"Loading initial URLs from: {UrlsFilePath}");
        Console.WriteLine($"Max concurrent requests: {MaxConcurrentRequests}");
        Console.WriteLine($"Delay between requests: {RequestDelayMilliseconds}ms");
        Console.WriteLine($"--------------------------------");

        var fileHandler = new FileHandler();
        if (!fileHandler.LoadUrlsFromFile(UrlsFilePath) || !fileHandler.Urls.Any())
        {
            Console.WriteLine("No URLs loaded. Exiting.");
            return;
        }

        var scraper = new OptimizedWebScraper(
            fileHandler.Urls,
            MaxConcurrentRequests,
            RequestDelayMilliseconds
        );

        Console.WriteLine("\nStarting scraping tasks...");
        var collectedUrls = await scraper.ScrapeAllSitesAsync();

        Console.WriteLine($"\nScraping finished. Total unique profile URLs collected: {collectedUrls.Count()}.");

        scraper.WriteResultsToFile(OutputFilePath);

        Console.WriteLine("\nWeb Scraping Process Ended.");
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
}