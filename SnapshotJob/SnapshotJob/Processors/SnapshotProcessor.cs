using Common.Logging;
using SnapshotJob.Data.Models;
using SnapshotJob.Data.Repositories;
using SnapshotJob.Models;

namespace SnapshotJob.Processors;

internal class SnapshotProcessor(ScrapeProcessor scrapeProcessor, 
    NewsSnapshotRepository newsSnapshotRepository,
    Logger logger)
{
    internal async Task Run()
    {
        NewsSnapshot snapshot = new() { StartedOn = DateTime.UtcNow };
        NewsSnapshotJob job = new() { NewsSnapshot = snapshot };

        try
		{
            // Insert initial News Snapshot record to track this session
            snapshot.Id = await newsSnapshotRepository.CreateAsync(snapshot.StartedOn.Value);

            job.ScrapeHeadlinesResult = await scrapeProcessor.ScrapeHeadlines(snapshot.Id);
            snapshot.SectionsScraped = job.ScrapeHeadlinesResult.SectionsScraped;
            snapshot.HeadlinesScraped = job.ScrapeHeadlinesResult.HeadlinesScraped;
            if (job.ScrapeHeadlinesResult.Exceptions is not null)
            {
                snapshot.ScrapeExceptions ??= [];
                snapshot.ScrapeExceptions.AddRange(job.ScrapeHeadlinesResult.Exceptions);
            }

            if (job.ScrapeHeadlinesResult.ScrapedHeadlines is not null)
            {
                job.ScrapeArticlesResult = await scrapeProcessor.ScrapeArticles([.. job.ScrapeHeadlinesResult.ScrapedHeadlines]);
                snapshot.ArticlesScraped = job.ScrapeArticlesResult.ArticlesScraped;
                if (job.ScrapeArticlesResult.ScrapedArticles is not null)
                    foreach (var article in job.ScrapeArticlesResult.ScrapedArticles.Where(a => a.Exceptions is not null))
                    {
                        snapshot.ScrapeExceptions ??= [];
                        snapshot.ScrapeExceptions.AddRange(article.Exceptions!);
                    }
            }

            snapshot.IsSuccess = true;
        }
		catch (Exception ex)
		{
            snapshot.IsSuccess = false;
            snapshot.SnapshotException = ex;
            throw;
        }
        finally
        {
            // Update job record with results
            snapshot.FinishedOn = DateTime.UtcNow;
            await newsSnapshotRepository.UpdateAsync(snapshot);

            // Log the results
            job.WriteToLog(logger);
            logger.Log($"\nNews snapshot job finished {(snapshot.IsSuccess!.Value ? "successfully" : "unsuccessfully")}.",
                messageLogLevel: (snapshot.IsSuccess!.Value ? LogLevel.Success : LogLevel.Error));
        }
    }
}
