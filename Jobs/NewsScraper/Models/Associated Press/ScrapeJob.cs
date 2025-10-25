using Common.Logging;
using NewsScraper.Models.AssociatedPress.ArticlePage;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Serialization;
using System;
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

    public int ArticlesScraped => ScrapedArticles.Count;

    public bool SkipArticlePageScrape { get; set; }
    public bool SkipMainPageScrape { get; set; }
    public bool UseArticlePageTestFile { get; set; }
    public bool UseMainPageTestFile { get; set; }
    public string? ArticlePageTestFile { get; set; }
    public string? MainPageTestFile { get; set; }

    public decimal? RunTimeInSeconds => JobFinishedOn.HasValue ? (decimal)((long)(JobFinishedOn.Value - JobStartedOn).TotalMilliseconds) / 1000 : null;

    public void WriteToLog(Logger logger)
    {
        if (UseMainPageTestFile)
            logger.Log($"\nScraping results from test file: {MainPageTestFile}", consoleColor: ConsoleColor.Yellow);
        else
            logger.Log($"\nScraping results from {SourceUri.AbsoluteUri}", consoleColor: ConsoleColor.Blue);

        if (ScrapeMainPageResult is null)
        {
            logger.Log("\nThere is no Main Page Scrape Result!\n", LogLevel.Error);
            return;
        }

        // Scrape Job Exception
        if (ScrapeJobException is not null)
        {
            logger.Log($"ScrapeJobException from {ScrapeJobException.Source}", LogLevel.Error, logAsRawMessage: true, ConsoleColor.DarkRed);
            logger.LogException(ScrapeJobException.Exception);
        }

        // Main Page Scrape Exceptions
        var mainPageExceptionCount = ScrapeMainPageResult.ScrapeExceptions.Count;
        logger.Log($"\nExceptions scraping the Main Page: {(mainPageExceptionCount > 0 ? mainPageExceptionCount : "None")}", 
            logAsRawMessage: true, consoleColor: (mainPageExceptionCount > 0 ? ConsoleColor.DarkRed : ConsoleColor.DarkGreen));
        int exceptionCount = 0;
        foreach (var exception in ScrapeMainPageResult.ScrapeExceptions)
        {
            logger.Log($"{++exceptionCount}: Exception in {exception.Source}", LogLevel.Error, logAsRawMessage: true);
            logger.LogException(exception.Exception);
        }

        // Article Page Scrape Exceptions
        var articlesWithException = ScrapedArticles.Where(a => a.ScrapeException is not null);
        logger.Log($"\nExceptions scraping Article Pages: {(articlesWithException.Any() ? articlesWithException.Count() : "None")}", 
            logAsRawMessage: true, consoleColor: (articlesWithException.Any() ? ConsoleColor.DarkRed : ConsoleColor.DarkGreen));
        int articleExceptionsCount = 0;
        foreach (var article in articlesWithException)
        {
            logger.Log($"{++articleExceptionsCount}: Exception in {article.ScrapeException?.Source}", LogLevel.Error, logAsRawMessage: true);
            logger.LogException(article.ScrapeException!.Exception);
        }

        // Page Sections and Headlines
        logger.Log($"\n{ScrapeMainPageResult.SectionsScraped} page sections found", logAsRawMessage: true, 
            consoleColor: (ScrapeMainPageResult.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        int headlineCount = 0;
        foreach (var headlineSection in ScrapeMainPageResult.Headlines.DistinctBy(a => a.SectionName))
        {
            string sectionName = $"{(string.IsNullOrWhiteSpace(headlineSection.SectionName) ? "No" : headlineSection.SectionName)} Section";
            logger.Log($"Section Name: {sectionName}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkCyan);
            var headlines = ScrapeMainPageResult.Headlines.Where(a => a.SectionName == headlineSection.SectionName);
            foreach (var headline in headlines)
            {
                logger.Log($"Headline {++headlineCount}:", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
                logger.Log(headline.ToString(), logAsRawMessage: true, consoleColor: ConsoleColor.Cyan);
            }
            logger.Log($"{headlines.Count()} headlines found in {sectionName}\n", logAsRawMessage: true,
                consoleColor: (ScrapeMainPageResult.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        }

        // Articles
        logger.Log($"{ArticlesScraped} articles found", logAsRawMessage: true, 
            consoleColor: (ArticlesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        int articleCount = 0;
        foreach (var article in ScrapedArticles)
        {
            logger.Log($"Article {++articleCount}:", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
            logger.Log(article.ToString(), logAsRawMessage: true, consoleColor: ConsoleColor.Cyan);
        }
        
        logger.Log($"\nTotal headlines found: {headlineCount}", logAsRawMessage: true,
            consoleColor: (headlineCount > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        logger.Log($"Sections scraped: {ScrapeMainPageResult.SectionsScraped}", logAsRawMessage: true,
            consoleColor: (ScrapeMainPageResult.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        logger.Log($"Headlines scraped: {ScrapeMainPageResult.HeadlinesScraped}", logAsRawMessage: true,
            consoleColor: (ScrapeMainPageResult.HeadlinesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        logger.Log($"Articles scraped: {ArticlesScraped}", logAsRawMessage: true,
            consoleColor: (ArticlesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));

        if (JobFinishedOn.HasValue)
            logger.Log($"Job took {RunTimeInSeconds} seconds", logAsRawMessage: true, consoleColor: ConsoleColor.Yellow);

        //logger.Log($"\n{ConsoleColor.Blue}", logAsRawMessage: true, consoleColor: ConsoleColor.Blue);
        //logger.Log($"{ConsoleColor.DarkBlue}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkBlue);
        //logger.Log($"{ConsoleColor.Cyan}", logAsRawMessage: true, consoleColor: ConsoleColor.Cyan);
        //logger.Log($"{ConsoleColor.DarkCyan}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkCyan);
        //logger.Log($"{ConsoleColor.Gray}", logAsRawMessage: true, consoleColor: ConsoleColor.Gray);
        //logger.Log($"{ConsoleColor.DarkGray}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkGray);
        //logger.Log($"{ConsoleColor.Green}", logAsRawMessage: true, consoleColor: ConsoleColor.Green);
        //logger.Log($"{ConsoleColor.DarkGreen}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkGreen);
        //logger.Log($"{ConsoleColor.Magenta}", logAsRawMessage: true, consoleColor: ConsoleColor.Magenta);
        //logger.Log($"{ConsoleColor.DarkMagenta}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkMagenta);
        //logger.Log($"{ConsoleColor.Red}", logAsRawMessage: true, consoleColor: ConsoleColor.Red);
        //logger.Log($"{ConsoleColor.DarkRed}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkRed);
        //logger.Log($"{ConsoleColor.Yellow}", logAsRawMessage: true, consoleColor: ConsoleColor.Yellow);
        //logger.Log($"{ConsoleColor.DarkYellow}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
