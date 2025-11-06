using Common.Data.Repositories;
using Common.Logging;
using Common.Models;

namespace SnapshotJob.Processors;

internal class SnapshotProcessor(ScrapeProcessor scrapeProcessor, 
    NewsSnapshotRepository newsSnapshotRepository,
    Logger logger)
{
    internal async Task Run()
    {
        NewsSnapshot snapshot = new() { StartedOn = DateTime.UtcNow };

        try
		{
            // Insert initial job record to track this session
            snapshot.Id = await newsSnapshotRepository.CreateAsync(snapshot);

            snapshot.ScrapeHeadlinesResult = await scrapeProcessor.ScrapeHeadlines(snapshot.Id);
            if (snapshot.ScrapeHeadlinesResult.ScrapedHeadlines is not null)
                snapshot.ScrapeArticlesResult = await scrapeProcessor.ScrapeArticles([.. snapshot.ScrapeHeadlinesResult.ScrapedHeadlines]);

            snapshot.IsSuccess = true;
        }
		catch (Exception ex)
		{
            snapshot.IsSuccess = false;
            snapshot.JobException = new JobException() { Source = $"{nameof(SnapshotProcessor)}.{nameof(Run)}", Exception = ex };
            throw;
        }
        finally
        {
            // Update job record with results
            snapshot.FinishedOn = DateTime.UtcNow;
            await newsSnapshotRepository.UpdateAsync(snapshot);

            // Log the results
            WriteToLog(snapshot);
            logger.Log($"\nNews snapshot job finished {(snapshot.IsSuccess!.Value ? "successfully" : "unsuccessfully")}.",
                messageLogLevel: (snapshot.IsSuccess!.Value ? LogLevel.Success : LogLevel.Error));
        }
    }

    public void WriteToLog(NewsSnapshot snapshot)
    {
        if (snapshot.JobException is not null)
        {
            logger.Log($"Snapshot Job Exception in {snapshot.JobException.Source}", LogLevel.Error);
            logger.LogException(snapshot.JobException.Exception);
        }

        if (snapshot.ScrapeHeadlinesResult is null)
        {
            logger.Log("Scraped headlines result is missing!", LogLevel.Error);
            return;
        }

        string source = snapshot.ScrapeHeadlinesResult.Source;
        bool testFileUsed = Uri.TryCreate(source, UriKind.Absolute, out Uri? sourceUri);
        if (testFileUsed)
            logger.Log($"\nScraping results from test file: {source}", consoleColor: ConsoleColor.Yellow);
        else
            logger.Log($"\nScraping results from {sourceUri?.AbsoluteUri}", consoleColor: ConsoleColor.Blue);

        // Scrape Exceptions
        var scrapeHeadlinesExceptions = snapshot.ScrapeHeadlinesResult.ScrapeExceptions;
        logger.Log($"\nScrape Exceptions: {(scrapeHeadlinesExceptions?.Count > 0 ? scrapeHeadlinesExceptions.Count : "None")}",
            logAsRawMessage: true, consoleColor: (scrapeHeadlinesExceptions?.Count > 0 ? ConsoleColor.DarkRed : ConsoleColor.DarkGreen));
        int scrapeExceptionCount = 0;
        if (scrapeHeadlinesExceptions is not null)
            foreach (var exception in scrapeHeadlinesExceptions)
            {
                logger.Log($"\n{++scrapeExceptionCount}: Exception in {exception.Source}", LogLevel.Error, logAsRawMessage: true);
                logger.LogException(exception.Exception);
            }

        if (snapshot.ScrapeArticlesResult is null)
        {
            logger.Log("Scraped articles result is missing!", LogLevel.Error);
            return;
        }

        // Article Scraping Exceptions
        if (snapshot.ScrapeArticlesResult.ScrapeException is not null)
        {
            logger.Log($"\nScrape Article Exception in {snapshot.ScrapeArticlesResult.ScrapeException?.Source}", 
                LogLevel.Error, logAsRawMessage: true);
            if (snapshot.ScrapeArticlesResult?.ScrapeException?.Exception is not null)
                logger.LogException(snapshot.ScrapeArticlesResult.ScrapeException.Exception);
        }
        var articlesWithException = snapshot.ScrapeArticlesResult?.ScrapedArticles?.Where(a => a.ScrapeExceptions is not null);
        logger.Log($"\nExceptions scraping articles: {(articlesWithException is not null ? articlesWithException.Count() : "None")}",
            logAsRawMessage: true, consoleColor: (articlesWithException is not null ? ConsoleColor.DarkRed : ConsoleColor.DarkGreen));
        int articleExceptionsCount = 0;
        if (articlesWithException is not null)
            foreach (var article in articlesWithException)
            {
                if (article.ScrapeExceptions is null)
                    continue;
                foreach (var exception in article.ScrapeExceptions)
                {
                    logger.Log($"\n{++articleExceptionsCount}: Exception in {exception.Source}", LogLevel.Error, logAsRawMessage: true);
                    logger.LogException(exception.Exception);
                }
            }

        // Page Sections and Headlines
        logger.Log($"\n{snapshot.ScrapeHeadlinesResult.SectionsScraped} page sections found", logAsRawMessage: true,
            consoleColor: (snapshot.ScrapeHeadlinesResult.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        int headlineCount = 0;
        if (snapshot.ScrapeHeadlinesResult.ScrapedHeadlines is not null)
            foreach (var headlineSection in snapshot.ScrapeHeadlinesResult.ScrapedHeadlines.DistinctBy(a => a.SectionName))
            {
                string sectionName = $"{(string.IsNullOrWhiteSpace(headlineSection.SectionName) ? "No" : headlineSection.SectionName)} Section";
                logger.Log($"Section Name: {sectionName}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkCyan);
                var headlines = snapshot.ScrapeHeadlinesResult.ScrapedHeadlines.Where(a => a.SectionName == headlineSection.SectionName);
                foreach (var headline in headlines)
                {
                    logger.Log($"Headline {++headlineCount}:", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
                    logger.Log(headline.ToString(), logAsRawMessage: true, consoleColor: ConsoleColor.Cyan);
                }
                logger.Log($"{headlines.Count()} headlines found in {sectionName}\n", logAsRawMessage: true,
                    consoleColor: (snapshot.ScrapeHeadlinesResult.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
            }

        // Articles
        logger.Log($"{snapshot.ScrapeArticlesResult?.ArticlesScraped} articles found", logAsRawMessage: true,
            consoleColor: (snapshot.ScrapeArticlesResult?.ArticlesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        int articleCount = 0;
        if (snapshot.ScrapeArticlesResult?.ScrapedArticles is not null)
            foreach (var article in snapshot.ScrapeArticlesResult.ScrapedArticles)
            {
                logger.Log($"Article {++articleCount}:", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
                logger.Log(article.ToString(), logAsRawMessage: true, consoleColor: (article.IsSuccess ? ConsoleColor.Cyan : ConsoleColor.DarkRed));
            }

        logger.Log($"\nTotal headlines found: {headlineCount}", logAsRawMessage: true,
            consoleColor: (headlineCount > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        logger.Log($"Sections scraped: {snapshot.ScrapeHeadlinesResult.SectionsScraped}", logAsRawMessage: true,
            consoleColor: (snapshot.ScrapeHeadlinesResult.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        logger.Log($"Headlines scraped: {snapshot.ScrapeHeadlinesResult.HeadlinesScraped}", logAsRawMessage: true,
            consoleColor: (snapshot.ScrapeHeadlinesResult.HeadlinesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        logger.Log($"Total Articles scraped: {snapshot.ScrapeArticlesResult?.ArticlesScraped}", logAsRawMessage: true,
            consoleColor: (snapshot.ScrapeArticlesResult?.ArticlesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        
        if (snapshot.FinishedOn.HasValue)
            logger.Log($"Job took {snapshot.RunTimeInSeconds} seconds", logAsRawMessage: true, consoleColor: ConsoleColor.Yellow);

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
}
