using Common.Logging;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using NewsScraper.Configuration.Options;
using NewsScraper.Models.CNN;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace NewsScraper.Scrapers.CNN;

internal class CnnArticleProvider(Logger logger, IOptions<PythonOptions> pythonOptions)
{
    private readonly PythonOptions _pythonOptions = pythonOptions.Value;

    private readonly string _getArticlesTestFile = @"C:\Users\danny\OneDrive\Projects\SnapshotNewsToday\TestData\CNN-Test-Landing-Page-2025-10-03-0200.html";
    private readonly bool _getArticlesUseTestFile = true;
    private readonly string _getArticleTestFile = @"C:\Users\danny\OneDrive\Projects\SnapshotNewsToday\TestData\CNN-test-article.html";
    private readonly bool _getArticleUseTestFile = true;

    public async Task<List<Article>> GetArticles()
    {
        string scriptPath = _pythonOptions.Scripts.GetNewsFromCnn;
        //scriptPath += $" --id {ScrapeJob.Id}";

        // FOR TESTING: Append test landing page file argument
        bool useTestLandingPageFile = _getArticlesUseTestFile;
        string testLandingPageFile = _getArticlesTestFile;
        if (useTestLandingPageFile && !string.IsNullOrEmpty(testLandingPageFile) && File.Exists(testLandingPageFile))
            scriptPath += $" --test-landing-page-file \"{testLandingPageFile}\"";

        List<Article> articles = [];
        var distinctArticles = new HashSet<Article>();

        // Run the Python script and parse its JSON output
        var jsonDocument = await RunPythonScript(scriptPath);
        foreach (var jsonElement in jsonDocument.RootElement.EnumerateArray())
        {
            //Uri.TryCreate($"{ScrapeJob.SourceUri}{jsonElement.GetProperty("url").GetString()}", UriKind.Absolute, out Uri? uri);
            //if (uri is null)
            //    continue; // Skip if URI is invalid

            //if (!DateTime.TryParse(jsonElement.GetProperty("publishdate").GetString(), out DateTime publishDate))
            //    continue; // Skip if publish date is invalid

            //distinctArticles.Add(new()
            //{
            //    ArticleUri = uri,
            //    PublishDate = publishDate,
            //    JobRunId = ScrapeJob.Id,
            //    SourceName = "CNN",
            //    Headline = jsonElement.GetProperty("headline").GetString()
            //});
        }

        // Group news articles by category and assign category to each article
        foreach (var grouped in GroupNewsArticlesByCategory([.. distinctArticles]))
            foreach (Article newsArticle in grouped.Value)
                newsArticle.Category = grouped.Key;

        return [.. distinctArticles.OrderBy(a => a.Category).ThenByDescending(a => a.PublishDate)];
    }

    public async Task GetArticle(Article article)
    {
        logger.Log($"Fetching article content from {article.ArticleUri}", LogLevel.Info);
        if (article.ArticleUri.AbsoluteUri.Contains("videos/"))
        {
            logger.Log($"Video articles are not supported. Article URL: {article.ArticleUri}", LogLevel.Warning);
            article.ErrorMessage = "Video articles are not supported.";
            article.Success = false;
            return;
        }

        HtmlDocument htmlDoc = new();
        if (_getArticleUseTestFile)   
        {
            string testArticleFile = _getArticleTestFile;
            if (string.IsNullOrEmpty(testArticleFile) || !File.Exists(testArticleFile))
            {
                logger.Log($"Test article file not found: {testArticleFile}", LogLevel.Error);
                throw new FileNotFoundException("Test article file not found.", testArticleFile);
            }
            logger.Log($"Loading article from test file: {testArticleFile}", LogLevel.Info);
            htmlDoc.Load(testArticleFile);
        }
        else
            htmlDoc.LoadHtml(await new HttpClient().GetStringAsync(article.ArticleUri.AbsoluteUri));
        
        // Extract publish date from data-first-publish attribute
        var timestampNode = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'timestamp__time-since')]");

        string? publishDateRaw = timestampNode?.GetAttributeValue("data-first-publish", string.Empty);
        if (DateTime.TryParse(publishDateRaw, out var parsedDate) == false)
            logger.Log($"Publish date not found or invalid. Raw Date: {publishDateRaw}", LogLevel.Warning);

        string? lastUpdatedDateRaw = timestampNode?.GetAttributeValue("data-last-publish", string.Empty);
        if (DateTime.TryParse(lastUpdatedDateRaw, out var parsedLastUpdatedDate) == false)
            logger.Log($"Last updated date not found or invalid. Raw Date: {lastUpdatedDateRaw}", LogLevel.Warning);

        // Extract author name  byline__name
        var authorNode = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'byline__name')]");

        // Extract headline
        var headlineNode = htmlDoc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'headline__text')]");

        // Populate Article object
        article.Headline = headlineNode?.InnerText.Trim();
        article.Author = authorNode?.InnerText.Trim();
        article.PublishDate = parsedDate.ToLocalTime();
        article.LastUpdatedDate = parsedLastUpdatedDate.ToLocalTime();
        article.Success = true;

        // Extract article content paragraphs
        var articleContentNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article__content')]");
        switch (articleContentNode is not null)
        {
            case true:
                foreach (var paragraph in articleContentNode.SelectNodes(".//p"))
                    article.ContentParagraphs.Add(paragraph.InnerText.Trim());
                break;
            case false:
                article.Success = false;
                article.ErrorMessage = "Article content not found.";
                logger.Log("Article content node not found.", LogLevel.Warning);
                break;
        }
    }

    private Dictionary<string, List<Article>> GroupNewsArticlesByCategory(List<Article> articles)
    {
        var groupedArticles = new Dictionary<string, List<Article>>();
        foreach (Article article in articles)
        {
            if (article.ArticleUri is null)
                continue; // Skip if Article or ArticleUri is null

            // Category is the 4th segment in path (assuming "/2025/09/28/category/...")
            string[] segments = article.ArticleUri.AbsolutePath.Trim('/').Split('/');
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
    private async Task<JsonDocument> RunPythonScript(string scriptPath)
    {
        var pythonScript = new ProcessStartInfo(_pythonOptions.PythonExePath)
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
