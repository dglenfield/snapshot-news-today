using Common.Logging;
using NewsScraper.Models.AssociatedPress.ArticlePage;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models.AssociatedPress;

/// <summary>
/// Represents the single execution of a job scraping Associated Press.
/// </summary>
public class ScrapeJob
{
    public long Id { get; set; }
    public required string SourceName { get; set; }
    public required Uri SourceUri { get; set; }
    public DateTime JobStartedOn { get; } = DateTime.UtcNow;
    public DateTime? JobFinishedOn { get; set; }
    public bool? IsSuccess { get; set; }
    public ScrapeException? ScrapeJobException { get; set; }
    public ScrapeMainPageResult? ScrapeMainPageResult { get; set; }
    public List<Article> ScrapedArticles { get; set; } = [];
    
    public bool SkipArticlePageScrape { get; set; }
    public bool SkipMainPageScrape { get; set; }
    public bool UseArticlePageTestFile { get; set; }
    public bool UseMainPageTestFile { get; set; }
    public string? ArticlePageTestFile { get; set; }
    public string? MainPageTestFile { get; set; }

    public void WriteToLog(Logger logger)
    {
        if (UseMainPageTestFile)
            logger.Log($"Scraping results from test file: {MainPageTestFile}");
        else
            logger.Log($"Scraping results from {SourceUri.AbsoluteUri}");

        if (ScrapeMainPageResult is null)
        {
            logger.Log("\nThere is no Main Page Scrape Result!\n", LogLevel.Error);
            return;
        }

        // Main Page Scrape Exceptions
        var mainPageExceptionCount = ScrapeMainPageResult.ScrapeExceptions.Count;
        logger.Log($"\nExceptions scraping the Main Page: {(mainPageExceptionCount > 0 ? mainPageExceptionCount : "None")}", logAsRawMessage: true);
        int exceptionCount = 0;
        foreach (var exception in ScrapeMainPageResult.ScrapeExceptions)
        {
            logger.Log($"{++exceptionCount}: Exception in {exception.Source}", LogLevel.Error, logAsRawMessage: true);
            logger.LogException(exception.Exception);
        }

        // Article Page Scrape Exceptions
        var articlesWithException = ScrapedArticles.Where(a => a.ScrapeException is not null);
        logger.Log($"\nExceptions scraping Article Pages: {(articlesWithException.Any() ? articlesWithException.Count() : "None")}", logAsRawMessage: true);
        int articeCount = 0;
        foreach (var article in articlesWithException)
        {
            logger.Log($"{++articeCount}: Exception in {article.ScrapeException?.Source}", LogLevel.Error, logAsRawMessage: true);
            logger.LogException(article.ScrapeException!.Exception);
        }

        // Page Sections and Headlines
        logger.Log($"\n{ScrapeMainPageResult.SectionsScraped} page sections found", logAsRawMessage: true);
        int headlineCount = 0;
        foreach (var headlineSection in ScrapeMainPageResult.Headlines.DistinctBy(a => a.SectionName))
        {
            logger.Log($"{headlineSection.SectionName} Section", logAsRawMessage: true);
            var headlines = ScrapeMainPageResult.Headlines.Where(a => a.SectionName == headlineSection.SectionName);
            foreach (var headline in headlines)
            {
                headlineCount++;
                logger.Log(headline.ToString(), logAsRawMessage: true);
            }
            logger.Log($"{headlines.Count()} headlines found in {headlineSection.SectionName}\n", logAsRawMessage: true);
        }

        logger.Log($"Total headlines found: {headlineCount}", logAsRawMessage: true);
        logger.Log($"Sections scraped: {ScrapeMainPageResult.SectionsScraped}", logAsRawMessage: true);
        logger.Log($"Headlines scraped: {ScrapeMainPageResult.HeadlinesScraped}", logAsRawMessage: true);
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
