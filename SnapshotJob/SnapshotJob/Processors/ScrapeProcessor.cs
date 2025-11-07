using SnapshotJob.Data.Models;
using SnapshotJob.Models;
using SnapshotJob.Scrapers.AssociatedPress.ArticlePage;
using SnapshotJob.Scrapers.MainPage;

namespace SnapshotJob.Processors;

internal class ScrapeProcessor(MainPageScraper mainPageScraper, ArticlePageScraper articleScraper)
{
    internal async Task<ScrapeArticlesResult> ScrapeArticles(List<ScrapedHeadline> scrapedHeadlines)
    {
        ScrapeArticlesResult result = new() { StartedOn = DateTime.UtcNow };
        
        try
        {
            // Scrape the full article for each headline
            int count = 0;
            foreach (var headline in scrapedHeadlines.Where(h => h.Id > 0))
            {
                Console.CursorLeft = 0;
                Console.Write($"Scraping article {++count} of {scrapedHeadlines.Count(h => h.Id > 0)} ");
                result.ScrapedArticles ??= [];
                result.ScrapedArticles.Add(await articleScraper.ScrapeAsync(headline));
                await Task.Delay(1000); // Throttle requests to the server
            }

            result.IsSuccess = true;
        }
        catch (Exception)
        {
            result.IsSuccess = false;
        }

        return result;
    }

    internal async Task<ScrapeHeadlinesResult> ScrapeHeadlines(long jobId)
    {
        // Scrape the main page
        return await mainPageScraper.ScrapeAsync(jobId);
    }
}
