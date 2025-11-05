using Common.Data.Repositories;
using Common.Logging;
using Common.Models;

namespace NewsSnapshot.Processors;

internal class SnapshotProcessor(ScrapeProcessor scrapeProcessor, 
    SnapshotJobRepository newsSnapshotJobRepository,
    Logger logger)
{
    internal async Task Run()
    {
        SnapshotJob job = new() { StartedOn = DateTime.UtcNow };

        try
		{
            // Insert initial job record to track this session
            job.Id = await newsSnapshotJobRepository.CreateAsync(job);

            job.ScrapeHeadlinesResult = await scrapeProcessor.ScrapeHeadlines(job.Id);
            if (job.ScrapeHeadlinesResult.ScrapedHeadlines is not null)
                job.ScrapeArticlesResult = await scrapeProcessor.ScrapeArticles([.. job.ScrapeHeadlinesResult.ScrapedHeadlines]);

            job.IsSuccess = true;
        }
		catch (Exception ex)
		{
            job.IsSuccess = false;
            job.JobException = new JobException() { Source = $"{nameof(SnapshotProcessor)}.{nameof(Run)}", Exception = ex };
            throw;
        }
        finally
        {
            // Update job record with results
            job.FinishedOn = DateTime.UtcNow;
            await newsSnapshotJobRepository.UpdateAsync(job);

            // Log the results
            job.WriteToLog(logger);
            logger.Log($"\nNews snapshot job finished {(job.IsSuccess!.Value ? "successfully" : "unsuccessfully")}.",
                messageLogLevel: (job.IsSuccess!.Value ? LogLevel.Success : LogLevel.Error));
        }
    }

    public void WriteToLog()
    {
        //bool testFileUsed = Uri.TryCreate(Source, UriKind.Absolute, out Uri? sourceUri);
        //if (testFileUsed)
        //    logger.Log($"\nScraping results from test file: {Source}", consoleColor: ConsoleColor.Yellow);
        //else
        //    logger.Log($"\nScraping results from {sourceUri?.AbsoluteUri}", consoleColor: ConsoleColor.Blue);

        //// Scrape Exceptions
        //logger.Log($"\nScrape Exceptions: {(ScrapeExceptions.Count > 0 ? ScrapeExceptions.Count : "None")}",
        //    logAsRawMessage: true, consoleColor: (ScrapeExceptions.Count > 0 ? ConsoleColor.DarkRed : ConsoleColor.DarkGreen));
        //int scrapeExceptionCount = 0;
        //foreach (var exception in ScrapeExceptions)
        //{
        //    logger.Log($"\n{++scrapeExceptionCount}: Exception in {exception.Source}", LogLevel.Error, logAsRawMessage: true);
        //    logger.LogException(exception.Exception);
        //}

        //// Article Page Scrape Exceptions
        //var articlesWithException = ScrapedArticles.Where(a => a.ScrapeException is not null);
        //logger.Log($"\nExceptions scraping Article Pages: {(articlesWithException.Any() ? articlesWithException.Count() : "None")}",
        //    logAsRawMessage: true, consoleColor: (articlesWithException.Any() ? ConsoleColor.DarkRed : ConsoleColor.DarkGreen));
        //int articleExceptionsCount = 0;
        //foreach (var articleEx in articlesWithException)
        //{
        //    logger.Log($"\n{++articleExceptionsCount}: Exception in {articleEx.ScrapeException?.Source}", LogLevel.Error, logAsRawMessage: true);
        //    logger.LogException(articleEx.ScrapeException!.Exception);
        //}

        //// Page Sections and Headlines
        //logger.Log($"\n{SectionsScraped} page sections found", logAsRawMessage: true,
        //    consoleColor: (SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        //int headlineCount = 0;
        //foreach (var headlineSection in ScrapedHeadlines.DistinctBy(a => a.SectionName))
        //{
        //    string sectionName = $"{(string.IsNullOrWhiteSpace(headlineSection.SectionName) ? "No" : headlineSection.SectionName)} Section";
        //    logger.Log($"Section Name: {sectionName}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkCyan);
        //    var headlines = ScrapedHeadlines.Where(a => a.SectionName == headlineSection.SectionName);
        //    foreach (var headline in headlines)
        //    {
        //        logger.Log($"Headline {++headlineCount}:", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
        //        logger.Log(headline.ToString(), logAsRawMessage: true, consoleColor: ConsoleColor.Cyan);
        //    }
        //    logger.Log($"{headlines.Count()} headlines found in {sectionName}\n", logAsRawMessage: true,
        //        consoleColor: (SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        //}

        //// Articles
        //logger.Log($"{TotalArticlesScraped} articles found", logAsRawMessage: true,
        //    consoleColor: (TotalArticlesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        //int articleCount = 0;
        //foreach (var article in ScrapedArticles)
        //{
        //    logger.Log($"Article {++articleCount}:", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
        //    logger.Log(article.ToString(), logAsRawMessage: true, consoleColor: (article.IsSuccess ? ConsoleColor.Cyan : ConsoleColor.DarkRed));
        //}

        //logger.Log($"\nTotal headlines found: {headlineCount}", logAsRawMessage: true,
        //    consoleColor: (headlineCount > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        //logger.Log($"Sections scraped: {SectionsScraped}", logAsRawMessage: true,
        //    consoleColor: (SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        //logger.Log($"Headlines scraped: {HeadlinesScraped}", logAsRawMessage: true,
        //    consoleColor: (HeadlinesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        //logger.Log($"Total Articles scraped: {TotalArticlesScraped}", logAsRawMessage: true,
        //    consoleColor: (TotalArticlesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));
        //logger.Log($"Articles successfully scraped: {ArticlesSuccessfullyScraped}", logAsRawMessage: true,
        //    consoleColor: (ArticlesSuccessfullyScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed));

        //if (FinishedOn.HasValue)
        //    logger.Log($"Job took {RunTimeInSeconds} seconds", logAsRawMessage: true, consoleColor: ConsoleColor.Yellow);

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
