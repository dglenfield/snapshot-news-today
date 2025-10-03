
using NewsScraper.Logging;
using NewsScraper.Models;
using System.Diagnostics;
using System.Text.Json;

namespace NewsScraper.Providers;

/// <summary>
/// Provides methods for scraping news articles and metadata from supported news websites.
/// </summary>
internal static class NewsProvider
{
    private static readonly string _cnnBaseUrl = Configuration.CnnBaseUrl;
    private static readonly string _pythonExePath = Configuration.PythonSettings.PythonExePath;
    private static readonly string _testLandingPageFile = Configuration.TestSettings.NewsProvider.GetNews.TestLandingPageFile;
    private static readonly bool _useTestLandingPageFile = Configuration.TestSettings.NewsProvider.GetNews.UseTestLandingPageFile;

    /// <summary>
    /// Retrieves a set of news article URLs from the specified news website.
    /// </summary>
    /// <param name="newsWebsite">The news website to scrape.</param>
    /// <returns>A set of unique article URLs, or null if none found.</returns>
    public static HashSet<Uri>? GetNews(NewsWebsite newsWebsite)
    {
        //if (_useTestLandingPageFile)
        //{
        //    Logger.Log("Using test file as per configuration.");
        //    return [new(_testLandingPageFile!)];
        //}

        return newsWebsite switch
        {
            NewsWebsite.CNN => GetNewsFromCNN(),
            NewsWebsite.FoxNews => throw new NotImplementedException("Fox News scraping not yet implemented."),
            _ => throw new NotSupportedException("Unsupported news website."),
        };
    }

    private static HashSet<Uri>? GetNewsFromCNN()
    {
        string scriptPath = Configuration.PythonSettings.GetNewsFromCnnScript;
        string arguments = scriptPath;
        if (_useTestLandingPageFile && !string.IsNullOrEmpty(_testLandingPageFile))
        {
            Logger.Log("Using test file as per configuration.");
            arguments += $" --test-landing-page-file \"{_testLandingPageFile}\"";
            Logger.Log($"Passing test landing page file: {_testLandingPageFile}");
        }
        Console.WriteLine($"Executing Python script: {_pythonExePath} {arguments}");
        var start = new ProcessStartInfo(_pythonExePath)
        {
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true
        };

        using var process = Process.Start(start) ?? throw new Exception("Failed to start Python process.");
        using var jsonDocument = JsonDocument.Parse(process.StandardOutput.ReadToEnd());
        
        List<Uri> urls = [];
        foreach (var jsonElement in jsonDocument.RootElement.EnumerateArray())
        {
            if (DateTime.TryParse(jsonElement.GetProperty("publishdate").GetString(), out DateTime publishDate))
            {
                // Return all articles regardless of age for now
                //if (publishDate <= DateTime.Today.AddDays(-1).AddHours(-12))
                //    continue; // Skip articles older than 1.5 days
            }
            else
            {
                Logger.Log($"Invalid publish date format: {jsonElement.GetProperty("publishdate")}", LogLevel.Warning);
                continue; // Skip if publish date is invalid
            }

            Uri.TryCreate($"{_cnnBaseUrl}{jsonElement.GetProperty("url").GetString()}", UriKind.Absolute, out Uri? uri);
            if (uri is not null)
                urls.Add(uri);

            NewsArticle article = new()
            {
                SourceName = "CNN",
                SourceUri = uri,
                SourceHeadline = jsonElement.GetProperty("headline").GetString(),
                SourcePublishDate = publishDate
            };
        }

        var distinctUrls = new HashSet<Uri>();
        foreach (var group in GroupUrlsByCategory(urls))
        {
            foreach (var url in group.Value.Take(5)) // Take top 5 from each category
                distinctUrls.Add(url);
        }

        return distinctUrls;
    }

    private static Dictionary<string, List<Uri>> GroupUrlsByCategory(List<Uri> urls)
    {
        var grouped = new Dictionary<string, List<Uri>>();
        foreach (var url in urls)
        {
            var segments = url.AbsolutePath.Trim('/').Split('/');
            
            // Category is the 4th segment in path (assuming "/2025/09/28/category/...")
            var category = segments.Length >= 4 ? segments[3] : "unknown";
            if (!grouped.ContainsKey(category))
                grouped[category] = [];
            
            grouped[category].Add(url);
        }

        return grouped;
    }
}
