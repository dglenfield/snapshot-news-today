using Common.Logging;
using Microsoft.Extensions.Options;
using NewsScraper.Configuration.Options;
using NewsScraper.Data;
using NewsScraper.Models;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Scrapers.AssociatedPress.MainPage;

namespace NewsScraper.Processors;

internal class AssociatePressProcessor(ScrapeJobRepository scrapeJobRepository, MainPageScraper mainPageScraper, Logger logger, 
    IOptions<NewsSourceOptions> newsSourceOptions)
{
    private readonly NewsSourceOptions _newsSourceOptions = newsSourceOptions.Value;

    internal async Task Run()
    {
        try
        {
            // Insert initial ScrapeJobRun record to track this scraping session
            ScrapeJob.Id = await scrapeJobRepository.CreateJobRunAsync();
            ScrapeJob.ScrapeStart = DateTime.UtcNow;

            ScrapeResult scrapeResult = await mainPageScraper.Scrape();

            //await apArticleProvider.GetArticle();

            // Log the scraping results
            LogScrapingResults(scrapeResult);

            //ScrapeJobRun.NewsArticlesFound = newsArticles.Count;
            //ScrapeJobRun.NewsArticlesScraped = newsArticles.Count(a => a.Success == true);
            ScrapeJob.Success = true;
        }
        catch (Exception ex)
        {
            ScrapeJob.Success = false;
            ScrapeJob.ErrorMessages = [ex.Message];
            throw;
        }
        finally
        {
            // Update ScrapeJob record with scrape results
            ScrapeJob.ScrapeEnd = DateTime.UtcNow;
            await scrapeJobRepository.UpdateJobRunAsync();
        }
    }

    private void LogScrapingResults(ScrapeResult result)
    {
        if (_newsSourceOptions.AssociatedPress.Scrapers.MainPage.UseTestFile)
            logger.Log($"Scraping results from test file: {_newsSourceOptions.AssociatedPress.Scrapers.MainPage.UseTestFile}");
        else
            logger.Log($"Scraping results from {ScrapeJob.SourceUri.AbsoluteUri}");

        logger.Log("\nScraping Exceptions:", logAsRawMessage: true);
        foreach (var section in result.Sections)
            if (section.ScrapeException is not null)
                logger.LogException(section.ScrapeException);

        logger.Log("\nScraping Messages:", logAsRawMessage: true);
        foreach (var section in result.Sections)
            if (section.ScrapeMessage is not null)
                if (section.ScrapeSuccess.HasValue && section.ScrapeSuccess.Value == false)
                    logger.Log(section.ScrapeMessage, LogLevel.Error, logAsRawMessage: true);
                else
                    logger.Log(section.ScrapeMessage, logAsRawMessage: true);

        logger.Log($"\n{result.Sections.Count} page sections found", logAsRawMessage: true);
        int articleCount = 0;
        foreach (var section in result.Sections)
        {
            logger.Log($"{section.Name} Section", logAsRawMessage: true);
            foreach (var article in section.Content)
            {
                articleCount++;
                if (article.LastUpdatedOn.HasValue)
                    logger.Log($"{article.LastUpdatedOn}", logAsRawMessage: true);
                logger.Log($"  {article.Title}", logAsRawMessage: true);
                logger.Log($"  {article.TargetUri}", logAsRawMessage: true);
            }
            logger.Log($"{section.Content.Count} articles found in {section.Name}\n", logAsRawMessage: true);
        }
        logger.Log($"Total articles found: {articleCount}", logAsRawMessage: true);
    }
}
