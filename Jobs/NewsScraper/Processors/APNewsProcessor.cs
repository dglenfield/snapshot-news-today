using Common.Logging;
using NewsScraper.Data.Repositories;
using NewsScraper.Models.AssociatedPress;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Scrapers.AssociatedPress.ArticlePage;
using NewsScraper.Scrapers.AssociatedPress.MainPage;

namespace NewsScraper.Processors;

internal class APNewsProcessor(APNewsScrapeJobRepository scrapeJobRepository, 
    MainPageScraper mainPageScraper, ArticlePageScraper articleScraper, Logger logger)
{
    internal async Task Run(ScrapeJob job)
    {
        try
        {
            // Insert initial ScrapeJobRun record to track this scraping session
            job.Id = await scrapeJobRepository.CreateAsync(job);

            // Scrape the main page
            job.ScrapeMainPageResult = await mainPageScraper.ScrapeAsync(job);

            // Scrape the full article for each headline
            foreach (var headline in job.ScrapeMainPageResult.Headlines.Where(h => h.Id > 0))
                job.ScrapedArticles.Add(await articleScraper.ScrapeAsync(headline, job));

            job.IsSuccess = true;
        }
        catch (Exception ex)
        {
            job.IsSuccess = false;
            job.ScrapeJobException = new ScrapeException() { Source = $"{nameof(APNewsProcessor)}.{nameof(Run)}", Exception = ex};
            throw;
        }
        finally
        {
            // Update ScrapeJob record with scrape results
            job.JobFinishedOn = DateTime.UtcNow;
            await scrapeJobRepository.UpdateAsync(job);

            // Log the scraping results
            job.WriteToLog(logger);
            logger.Log($"AP News scraping job finished {(job.IsSuccess!.Value ? "successfully" : "unsuccessfully")}.",
                messageLogLevel: (job.IsSuccess!.Value ? LogLevel.Success : LogLevel.Error));
        }
    }
}
