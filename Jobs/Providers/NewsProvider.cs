using NewsScraper.Logging;
using NewsScraper.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace NewsScraper.Providers;

/// <summary>
/// Provides methods for scraping news articles and metadata from supported news websites.
/// </summary>
internal class NewsProvider
{
    private readonly string _cnnBaseUrl = Configuration.CnnBaseUrl;
    private readonly string _pythonExePath = Configuration.PythonSettings.PythonExePath;

    /// <summary>
    /// Retrieves a set of news article URLs from the specified news website.
    /// </summary>
    /// <param name="newsWebsite">The news website to scrape.</param>
    /// <returns>A set of unique article URLs, or null if none found.</returns>
    public List<NewsArticle> GetNewsArticles(NewsWebsite newsWebsite)
    {
        return newsWebsite switch
        {
            NewsWebsite.CNN => GetArticlesFromCNN(),
            NewsWebsite.FoxNews => throw new NotImplementedException("Fox News scraping not yet implemented."),
            _ => throw new NotSupportedException("Unsupported news website."),
        };
    }

    private List<NewsArticle> GetArticlesFromCNN()
    {
        string scriptPath = Configuration.PythonSettings.GetNewsFromCnnScript;

        // FOR TESTING: Append test landing page file argument
        bool useTestLandingPageFile = Configuration.TestSettings.NewsProvider.GetNews.UseTestLandingPageFile;
        string testLandingPageFile = Configuration.TestSettings.NewsProvider.GetNews.TestLandingPageFile;
        if (useTestLandingPageFile && !string.IsNullOrEmpty(testLandingPageFile) && File.Exists(testLandingPageFile))
            scriptPath += $" --test-landing-page-file \"{testLandingPageFile}\"";

        List<NewsArticle> articles = [];
        var distinctArticles = new HashSet<NewsArticle>();
        foreach (var jsonElement in RunPythonScript(scriptPath).RootElement.EnumerateArray())
        {
            Uri.TryCreate($"{_cnnBaseUrl}{jsonElement.GetProperty("url").GetString()}", UriKind.Absolute, out Uri? uri);
            if (uri is null)
                continue; // Skip if URI is invalid

            if (!DateTime.TryParse(jsonElement.GetProperty("publishdate").GetString(), out DateTime publishDate))
                continue; // Skip if publish date is invalid

            distinctArticles.Add(new()
            {
                SourceName = "CNN",
                SourceUri = uri,
                SourceHeadline = jsonElement.GetProperty("headline").GetString(),
                SourcePublishDate = publishDate
            });
        }

        // Group articles by category and assign category to each article
        foreach (var grouped in GroupArticlesByCategory([.. distinctArticles]))
            foreach (NewsArticle article in grouped.Value)
                article.SourceCategory = grouped.Key;

        return [.. distinctArticles.OrderBy(a => a.SourceCategory).ThenByDescending(a => a.SourcePublishDate)];
    }

    /// <summary>
    /// Groups a collection of news articles by their category, as determined from the article's source URI path.
    /// </summary>
    /// <remarks>The category is extracted as the fourth segment of the article's source URI path. If the path
    /// does not contain at least four segments, the article is assigned to the "unknown" category.</remarks>
    /// <param name="articles">The list of news articles to group. Cannot be null.</param>
    /// <returns>A dictionary where each key is a category name and the value is a list of articles belonging to that category.
    /// Articles with an unrecognized or missing category are grouped under the key "unknown".</returns>
    private Dictionary<string, List<NewsArticle>> GroupArticlesByCategory(List<NewsArticle> articles)
    {
        var groupedArticles = new Dictionary<string, List<NewsArticle>>();
        foreach (NewsArticle article in articles)
        {
            // Category is the 4th segment in path (assuming "/2025/09/28/category/...")
            string[] segments = article.SourceUri.AbsolutePath.Trim('/').Split('/');
            string category = segments.Length >= 4 ? segments[3] : "unknown";
            if (!groupedArticles.ContainsKey(category))
                groupedArticles[category] = [];
            groupedArticles[category].Add(article);
        }
        return groupedArticles;
    }

    /// <summary>
    /// Runs a Python script located at the specified path and parses its standard output as a JSON document.
    /// </summary>
    /// <remarks>The Python script must write valid JSON to its standard output. Any errors in script
    /// execution or invalid JSON output will result in an exception.</remarks>
    /// <param name="scriptPath">The full file path to the Python script to execute. Cannot be null or empty.</param>
    /// <returns>A JsonDocument representing the parsed JSON output from the Python script's standard output.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Python process cannot be started.</exception>
    private JsonDocument RunPythonScript(string scriptPath)
    {
        var pythonScript = new ProcessStartInfo(_pythonExePath)
        {
            Arguments = scriptPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true
        };

        try
        {
            using var process = Process.Start(pythonScript) ?? throw new InvalidOperationException("Failed to start Python process.\nScript: {scriptPath}");
            return JsonDocument.Parse(process.StandardOutput.ReadToEnd());
        }
        catch (Win32Exception ex)
        {
            Logger.Log($"Failed to start process: {ex.Message}\nScript: {scriptPath}\n", LogLevel.Error);
            throw;
        }
        catch (JsonException ex)
        {
            Logger.Log($"JSON parsing failed: {ex.Message}\nScript: {scriptPath}\n", LogLevel.Error);
            throw;
        }
    }
}
