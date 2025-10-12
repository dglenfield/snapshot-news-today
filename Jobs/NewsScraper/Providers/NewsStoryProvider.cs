using Common.Logging;
using NewsScraper.Enums;
using NewsScraper.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace NewsScraper.Providers;

/// <summary>
/// Provides methods for scraping news stories from supported news websites.
/// </summary>
internal class NewsStoryProvider(string cnnBaseUrl, string pythonExePath, Logger logger)
{
    public async Task<List<SourceNewsStory>> GetNewsStories(NewsWebsite newsWebsite)
    {
        return newsWebsite switch
        {
            NewsWebsite.CNN => await GetFromCNN(),
            NewsWebsite.FoxNews => throw new NotImplementedException("Fox News scraping not yet implemented."),
            _ => throw new NotSupportedException("Unsupported news website."),
        };
    }

    private async Task<List<SourceNewsStory>> GetFromCNN()
    {
        string scriptPath = Configuration.PythonSettings.GetNewsFromCnnScript;
        scriptPath += $" --id {ScrapeJobRun.Id}";
        if (Configuration.Database.NewsScraperJobRaw.IsEnabled)
            scriptPath += $" --db-path {Configuration.Database.NewsScraperJobRaw.DatabaseFilePath}";

        // FOR TESTING: Append test landing page file argument
        bool useTestLandingPageFile = Configuration.TestSettings.NewsStoryProvider.GetNews.UseTestLandingPageFile;
        string testLandingPageFile = Configuration.TestSettings.NewsStoryProvider.GetNews.TestLandingPageFile;
        if (useTestLandingPageFile && !string.IsNullOrEmpty(testLandingPageFile) && File.Exists(testLandingPageFile))
            scriptPath += $" --test-landing-page-file \"{testLandingPageFile}\"";
        
        List<SourceNewsStory> articles = [];
        var distinctNewsStories = new HashSet<SourceNewsStory>();

        // Run the Python script and parse its JSON output
        var jsonDocument = await RunPythonScript(scriptPath);
        foreach (var jsonElement in jsonDocument.RootElement.EnumerateArray())
        {
            Uri.TryCreate($"{cnnBaseUrl}{jsonElement.GetProperty("url").GetString()}", UriKind.Absolute, out Uri? uri);
            if (uri is null)
                continue; // Skip if URI is invalid

            if (!DateTime.TryParse(jsonElement.GetProperty("publishdate").GetString(), out DateTime publishDate))
                continue; // Skip if publish date is invalid

            distinctNewsStories.Add(new()
            {
                Article = new SourceArticle { ArticleUri = uri, PublishDate = publishDate },
                JobRunId = ScrapeJobRun.Id,
                SourceName = "CNN",
                StoryHeadline = jsonElement.GetProperty("headline").GetString()
            });
        }
        
        // Group news stories by category and assign category to each article
        foreach (var grouped in GroupNewsStoriesByCategory([.. distinctNewsStories]))
            foreach (SourceNewsStory newsStory in grouped.Value)
                newsStory.Category = grouped.Key;

        return [.. distinctNewsStories.OrderBy(a => a.Category).ThenByDescending(a => a.Article?.PublishDate)];
    }

    /// <summary>
    /// Groups a collection of news stories by their category, as determined from the article's source URI path.
    /// </summary>
    /// <remarks>The category is extracted as the fourth segment of the article's source URI path. If the path
    /// does not contain at least four segments, the article is assigned to the "unknown" category.</remarks>
    /// <param name="stories">The list of news stories to group. Cannot be null.</param>
    /// <returns>A dictionary where each key is a category name and the value is a list of news stories belonging to that category.
    /// News stories with an unrecognized or missing category are grouped under the key "unknown".</returns>
    private Dictionary<string, List<SourceNewsStory>> GroupNewsStoriesByCategory(List<SourceNewsStory> stories)
    {
        var groupedStories = new Dictionary<string, List<SourceNewsStory>>();
        foreach (SourceNewsStory story in stories)
        {
            if (story.Article?.ArticleUri is null)
                continue; // Skip if Article or ArticleUri is null

            // Category is the 4th segment in path (assuming "/2025/09/28/category/...")
            string[] segments = story.Article.ArticleUri.AbsolutePath.Trim('/').Split('/');
            string category = segments.Length >= 4 ? segments[3] : "unknown";
            if (!groupedStories.ContainsKey(category))
                groupedStories[category] = [];
            groupedStories[category].Add(story);
        }
        return groupedStories;
    }

    /// <summary>
    /// Runs a Python script located at the specified path and parses its standard output as a JSON document.
    /// </summary>
    /// <remarks>The Python script must write valid JSON to its standard output. Any errors in script
    /// execution or invalid JSON output will result in an exception.</remarks>
    /// <param name="scriptPath">The full file path to the Python script to execute. Cannot be null or empty.</param>
    /// <returns>A JsonDocument representing the parsed JSON output from the Python script's standard output.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Python process cannot be started.</exception>
    private async Task<JsonDocument> RunPythonScript(string scriptPath)
    {
        var pythonScript = new ProcessStartInfo(pythonExePath)
        {
            Arguments = scriptPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true
        };

        try
        {
            using var process = Process.Start(pythonScript) ?? throw new InvalidOperationException("Failed to start Python process.\nScript: {scriptPath}");
            var output = process.StandardOutput.ReadToEnd();
            return JsonDocument.Parse(output);
        }
        catch (Win32Exception ex)
        {
            logger.Log($"Failed to start process: {ex.Message}\nScript: {scriptPath}\n", LogLevel.Error);
            throw;
        }
        catch (JsonException ex)
        {
            logger.Log($"JSON parsing failed: {ex.Message}\nScript: {scriptPath}\n", LogLevel.Error);
            throw;
        }
    }
}
