using Common.Logging;
using Common.Serialization;
using SnapshotJob.Data.Models;
using System.Text.Json;

namespace SnapshotJob.Models;

public class NewsSnapshotJob
{
    public NewsSnapshot? NewsSnapshot { get; set; }
    public ScrapeArticlesResult? ScrapeArticlesResult { get; set; }
    public ScrapeHeadlinesResult? ScrapeHeadlinesResult { get; set; }

    public void WriteToLog(Logger logger)
    {
        bool testFileUsed = Uri.TryCreate(ScrapeHeadlinesResult?.Source, UriKind.Absolute, out Uri? sourceUri);
        if (testFileUsed)
            logger.Log($"\nScraping results from test file: {ScrapeHeadlinesResult?.Source}", consoleColor: ConsoleColor.Yellow);
        else
            logger.Log($"\nScraping results from {sourceUri?.AbsoluteUri}", consoleColor: ConsoleColor.Blue);

        // News Snapshot Exception
        if (NewsSnapshot?.SnapshotException is not null)
        {
            logger.Log($"\nNews Snapshot Exception in {NewsSnapshot.SnapshotException.Source}", LogLevel.Error, logAsRawMessage: true);
            logger.LogException(NewsSnapshot.SnapshotException);
        }

        // Scrape Headlines Exceptions
        if (ScrapeHeadlinesResult?.Exceptions is not null)
        {
            var exceptions = ScrapeHeadlinesResult.Exceptions;
            logger.Log($"\nScrape Headlines Exceptions: {(exceptions?.Count > 0 ? exceptions.Count : "None")}",
                consoleColor: exceptions?.Count > 0 ? ConsoleColor.DarkRed : ConsoleColor.DarkGreen, logAsRawMessage: true);
            int scrapeExceptionCount = 0;
            if (exceptions is not null)
            {
                foreach (var exception in exceptions)
                {
                    logger.Log($"\n{++scrapeExceptionCount}: Exception in {exception.Source}", LogLevel.Error, logAsRawMessage: true);
                    logger.LogException(exception);
                }
            }
        }
        
        // Scrape Articles Exceptions
        var articlesWithException = ScrapeArticlesResult?.ScrapedArticles?.Where(a => a.Exceptions is not null);
        logger.Log($"\nArticles with Exceptions: {(articlesWithException is null ? "None" : articlesWithException.Count())}",
            consoleColor: articlesWithException is null ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed, logAsRawMessage: true);
        if (articlesWithException is not null)
        {
            int exceptionCount = 0;
            foreach (var article in articlesWithException)
            {
                foreach (var exception in article.Exceptions!)
                {
                    logger.Log($"\n{++exceptionCount}: Exception in {exception.Source}", LogLevel.Error, logAsRawMessage: true);
                    logger.LogException(exception);
                }
            }
        }
            
        // Page Sections and Headlines
        logger.Log($"\n{ScrapeHeadlinesResult?.SectionsScraped} page sections found", logAsRawMessage: true,
            consoleColor: ScrapeHeadlinesResult?.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
        if (ScrapeHeadlinesResult?.ScrapedHeadlines is not null)
        {
            int headlineCount = 0;
            foreach (var section in ScrapeHeadlinesResult.ScrapedHeadlines.DistinctBy(a => a.SectionName))
            {
                string sectionName = $"{(string.IsNullOrWhiteSpace(section.SectionName) ? "No" : section.SectionName)} Section";
                logger.Log($"Section Name: {sectionName}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkCyan);
                var headlines = ScrapeHeadlinesResult.ScrapedHeadlines.Where(a => a.SectionName == section.SectionName);
                foreach (var headline in headlines)
                {
                    logger.Log($"Headline {++headlineCount}:", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
                    logger.Log(headline.ToString(), logAsRawMessage: true, consoleColor: ConsoleColor.Cyan);
                }
                logger.Log($"{headlines.Count()} headlines found in {sectionName}\n", logAsRawMessage: true,
                    consoleColor: ScrapeHeadlinesResult.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
            }
        }
        
        // Articles
        logger.Log($"{ScrapeArticlesResult?.ArticlesScraped} articles found", logAsRawMessage: true,
            consoleColor: ScrapeArticlesResult?.ArticlesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
        if (ScrapeArticlesResult?.ScrapedArticles is not null)
        {
            int articleCount = 0;
            foreach (var article in ScrapeArticlesResult.ScrapedArticles)
            {
                logger.Log($"Article {++articleCount}:", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
                logger.Log(article.ToString(), logAsRawMessage: true, consoleColor: (article.IsSuccess ? ConsoleColor.Cyan : ConsoleColor.DarkRed));
            }
        }
        
        logger.Log($"Sections scraped: {ScrapeHeadlinesResult?.SectionsScraped}", logAsRawMessage: true,
            consoleColor: ScrapeHeadlinesResult?.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
        logger.Log($"Headlines scraped: {ScrapeHeadlinesResult?.HeadlinesScraped}", logAsRawMessage: true,
            consoleColor: ScrapeHeadlinesResult?.HeadlinesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
        logger.Log($"Total Articles scraped: {ScrapeArticlesResult?.ArticlesScraped}", logAsRawMessage: true,
            consoleColor: ScrapeArticlesResult?.ArticlesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);

        if (NewsSnapshot is not null && NewsSnapshot.FinishedOn.HasValue)
            logger.Log($"Job took {NewsSnapshot.RunTimeInSeconds} seconds", logAsRawMessage: true, consoleColor: ConsoleColor.Yellow);

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
