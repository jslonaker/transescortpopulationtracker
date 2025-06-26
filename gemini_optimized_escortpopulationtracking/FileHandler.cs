// FileHandler.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebScrapingApp
{
    public class FileHandler
    {
        public List<string> Urls { get; private set; }

        public FileHandler()
        {
            Urls = new List<string>();
        }

        public bool LoadUrlsFromFile(string filePath)
        {
            Urls.Clear();
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found at '{filePath}'");
                return false;
            }

            try
            {
                // Assuming simple CSV with one URL per line, or specific column
                // Adjust parsing if your CSV is more complex
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    // Basic validation: ensure it's not empty and looks like a URL
                    if (!string.IsNullOrWhiteSpace(line) && (line.StartsWith("http://") || line.StartsWith("https://")))
                    {
                        Urls.Add(line.Trim());
                    }
                }
                Console.WriteLine($"Loaded {Urls.Count} URLs from '{filePath}'.");
                return Urls.Any();
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading file '{filePath}': {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred while loading URLs: {ex.Message}");
                return false;
            }
        }
    }
}