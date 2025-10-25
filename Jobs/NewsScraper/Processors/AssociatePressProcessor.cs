using Common.Logging;
using NewsScraper.Data.Repositories;
using NewsScraper.Models.AssociatedPress;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Scrapers.AssociatedPress.ArticlePage;
using NewsScraper.Scrapers.AssociatedPress.MainPage;

namespace NewsScraper.Processors;

internal class AssociatePressProcessor(ScrapeAssociatedPressJobRepository scrapeJobRepository, 
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
            {
                job.ScrapedArticles.Add(await articleScraper.ScrapeAsync(headline, job.UseArticlePageTestFile, job.ArticlePageTestFile));
                if (job.UseArticlePageTestFile)
                    break;
            }

            // Log the scraping results
            job.WriteToLog(logger);

            job.IsSuccess = true;
        }
        catch (Exception ex)
        {
            job.IsSuccess = false;
            job.ScrapeJobException = new ScrapeException() { Source = $"{nameof(Run)}", Exception = ex};
            throw;
        }
        finally
        {
            // Update ScrapeJob record with scrape results
            job.JobFinishedOn = DateTime.UtcNow;
            await scrapeJobRepository.UpdateAsync(job);
        }
    }
}
