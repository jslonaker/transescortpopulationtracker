// OptimizedWebScraper.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WebScrapingApp
{
    public class OptimizedWebScraper
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly SemaphoreSlim _concurrencySemaphore;
        private readonly HashSet<string> _collectedProfileUrls; // Using HashSet for automatic duplicate handling and efficiency
        private readonly List<string> _initialUrls;
        private readonly int _requestDelayMs;

        public OptimizedWebScraper(List<string> initialUrls, int maxConcurrentRequests = 10, int requestDelayMs = 200)
        {
            _initialUrls = initialUrls ?? throw new ArgumentNullException(nameof(initialUrls));
            _concurrencySemaphore = new SemaphoreSlim(maxConcurrentRequests);
            _collectedProfileUrls = new HashSet<string>();
            _requestDelayMs = requestDelayMs;

            // Configure HttpClient - good practice to set a User-Agent
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _httpClient.Timeout = TimeSpan.FromSeconds(60); // Set a reasonable timeout
        }

        public async Task<IEnumerable<string>> ScrapeAllSitesAsync()
        {
            _collectedProfileUrls.Clear();
            var scrapingTasks = new List<Task>();

            foreach (var url in _initialUrls)
            {
                scrapingTasks.Add(ProcessTopUrlAsync(url));
            }

            await Task.WhenAll(scrapingTasks);
            return _collectedProfileUrls.ToList(); // Return a list copy
        }

        private async Task<HtmlDocument> FetchHtmlDocumentAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            await Task.Delay(_requestDelayMs); // Polite delay before each request

            await _concurrencySemaphore.WaitAsync();
            try
            {
                Console.WriteLine($"Fetching: {url}");
                var responseString = await _httpClient.GetStringAsync(url);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(responseString);
                return htmlDoc;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error fetching {url}: {ex.StatusCode} - {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex) // Catches timeouts
            {
                Console.WriteLine($"Timeout fetching {url}: {ex.Message}");
                return null;
            }
            catch (Exception ex) // Catch other potential errors (e.g., parsing errors if LoadHtml fails)
            {
                Console.WriteLine($"Generic error for {url}: {ex.Message}");
                return null;
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        private async Task ProcessTopUrlAsync(string topUrl)
        {
            HtmlDocument initialDoc = await FetchHtmlDocumentAsync(topUrl);
            if (initialDoc == null)
            {
                Console.WriteLine($"Skipping {topUrl} due to fetch failure.");
                return;
            }

            var pagesToProcess = new List<(string Url, HtmlDocument Document)> { (topUrl, initialDoc) };
            var processedPageUrls = new HashSet<string> { topUrl }; // Keep track of URLs for this topUrl's scope

            // Discover paginated pages
            var paginationNodes = initialDoc.DocumentNode.SelectNodes("//ul[@class='pagination list-unstyled']//li");
            if (paginationNodes != null)
            {
                // Your original pagination logic: count - 2
                // This assumes the structure has non-page items at the start/end of pagination.
                // More robust: parse hrefs and check for actual page numbers.
                int pageLinkItemsCount = paginationNodes.Count - 2; // As per your original logic

                for (int i = 0; i < pageLinkItemsCount; i++)
                {
                    // Your original logic: "?page=(i + 1)"
                    string paginatedUrl = $"{topUrl}?page={i + 1}";
                    if (processedPageUrls.Add(paginatedUrl)) // Ensure we haven't processed this pagination URL
                    {
                        HtmlDocument paginatedDoc = await FetchHtmlDocumentAsync(paginatedUrl);
                        if (paginatedDoc != null)
                        {
                            pagesToProcess.Add((paginatedUrl, paginatedDoc));
                        }
                    }
                }
            }

            // Extract profile URLs from all collected pages for this topUrl
            foreach (var pageData in pagesToProcess)
            {
                ExtractProfileLinksFromDocument(pageData.Document, pageData.Url);
            }
        }

        private void ExtractProfileLinksFromDocument(HtmlDocument htmlDoc, string sourceUrl)
        {
            var userProfileNodes = htmlDoc.DocumentNode.SelectNodes("//a[@class='eitem']");
            if (userProfileNodes == null || !userProfileNodes.Any())
            {
                Console.WriteLine($"No user profiles found at: {sourceUrl}");
                return;
            }

            int countBeforeAdd = 0;
            int addedNow = 0;

            foreach (var node in userProfileNodes)
            {
                string profileUrl = node.GetAttributeValue("href", null);
                if (!string.IsNullOrWhiteSpace(profileUrl))
                {
                    // Ensure URLs are absolute if they are relative
                    // Uri baseUri = new Uri(sourceUrl);
                    // Uri absoluteUri = new Uri(baseUri, profileUrl);
                    // string absoluteProfileUrl = absoluteUri.AbsoluteUri;

                    // For now, assuming profileUrl is already absolute or correct as is.
                    // If they can be relative, you'll need to resolve them using the sourceUrl.

                    bool newlyAdded;
                    lock (_collectedProfileUrls) // HashSet Add is O(1) on average but not thread-safe for concurrent writes
                    {
                        countBeforeAdd = _collectedProfileUrls.Count;
                        newlyAdded = _collectedProfileUrls.Add(profileUrl);
                    }

                    if (newlyAdded)
                    {
                        addedNow++;
                        Console.WriteLine($"Added: {profileUrl} (Total unique: {_collectedProfileUrls.Count})");
                    }
                }
            }
            if (addedNow > 0)
            {
                Console.WriteLine($"Found {addedNow} new profile links on {sourceUrl}.");
            }
        }

        public void WriteResultsToFile(string filePath)
        {
            if (!_collectedProfileUrls.Any())
            {
                Console.WriteLine("No profile URLs were collected to write.");
                return;
            }

            try
            {
                File.WriteAllLines(filePath, _collectedProfileUrls);
                Console.WriteLine($"Successfully wrote {_collectedProfileUrls.Count} unique profile URLs to '{filePath}'.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error writing results to file '{filePath}': {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred while writing results: {ex.Message}");
            }
        }
    }
}