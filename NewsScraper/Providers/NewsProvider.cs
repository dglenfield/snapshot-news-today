using Microsoft.Extensions.Configuration;
using NewsScraper.Utilities;
using System.Diagnostics;
using System.Text.Json;

namespace NewsScraper.Providers;

/// <summary>
/// Provides methods for scraping news articles and metadata from supported news websites.
/// </summary>
internal static class NewsProvider
{
    private static readonly string _cnnBaseUrl;
    private static readonly string _debugUrl;
    private static readonly string _pythonPath;
    private static readonly bool _useDebugUrl;

    static NewsProvider()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<Program>()
            .Build();

        _cnnBaseUrl = config["NewsProvider:CnnBaseUrl"] ?? throw new InvalidOperationException("NewsProvider:CnnBaseUrl not found in appsettings.");
        _pythonPath = config["PythonPath"] ?? throw new InvalidOperationException("PythonPath not found in appsettings.");
        _debugUrl = config["NewsProvider:DebugArticleUrl"] ?? string.Empty;
        _useDebugUrl = bool.Parse(config["NewsProvider:UseDebugArticle"] ?? "false");

        if (_useDebugUrl && string.IsNullOrWhiteSpace(_debugUrl))
            throw new InvalidOperationException("NewsProvider:DebugArticleUrl must be set when UseDebugArticle is true.");
    }

    /// <summary>
    /// Retrieves a set of news article URLs from the specified news website.
    /// </summary>
    /// <param name="newsWebsite">The news website to scrape.</param>
    /// <returns>A set of unique article URLs, or null if none found.</returns>
    public static HashSet<Uri>? GetNews(NewsWebsite newsWebsite)
    {
        if (_useDebugUrl)
        {
            Logger.Log("Using debug URL as per configuration.", LogLevel.Info);
            return [new(_debugUrl)];
        }

        switch (newsWebsite)
        {
            case NewsWebsite.CNN:
                return GetNewsFromCNN();
            case NewsWebsite.FoxNews:
                Logger.Log("Fox News scraping not yet implemented.", LogLevel.Warning);
                throw new NotImplementedException("Fox News scraping not yet implemented.");
            default:
                throw new NotSupportedException("Unsupported news website.");
        }
    }

    private static HashSet<Uri>? GetNewsFromCNN()
    {
        string scriptPath = @"C:\Repos\snapshot-news-today\NewsScraper\Python\cnn_parser.py";

        var start = new ProcessStartInfo(_pythonPath)
        {
            Arguments = scriptPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true
        };

        using var process = Process.Start(start) ?? throw new Exception("Failed to start Python process.");
        using var doc = JsonDocument.Parse(process.StandardOutput.ReadToEnd());

        List<Uri> urls = [];
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (DateTime.TryParse(item.GetProperty("publishdate").ToString(), out DateTime publishDate))
            {
                if (publishDate < DateTime.Now.AddDays(-1))
                    continue; // Skip articles older than 1 day
            }
            else
            {
                Logger.Log($"Invalid publish date format: {item.GetProperty("publishdate")}", LogLevel.Warning);
                continue; // Skip if publish date is invalid
            }

            Uri.TryCreate($"{_cnnBaseUrl}{item.GetProperty("url").GetString()}", UriKind.Absolute, out Uri? uri);
            if (uri is not null)
                urls.Add(uri);
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
