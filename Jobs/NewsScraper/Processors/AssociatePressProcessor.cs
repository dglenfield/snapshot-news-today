using Common.Logging;
using NewsScraper.Data;
using NewsScraper.Models;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Scrapers.AssociatedPress.MainPage;

namespace NewsScraper.Processors;

internal class AssociatePressProcessor(ScrapeJobRepository scrapeJobRepository, AssociatedPressHeadlineRepository headlineRepository,
    MainPageScraper mainPageScraper, Logger logger)
{
    internal async Task Run(ScrapeJob job)
    {
        try
        {
            // Insert initial ScrapeJobRun record to track this scraping session
            job.Id = await scrapeJobRepository.CreateScrapeJobAsync(job);

            // Scrape the main page
            job.PageScrapeResult = await mainPageScraper.Scrape(job.SourceUri, job.UseTestFile, job.TestFile);
            foreach (var headline in job.PageScrapeResult.Headlines)
            {
                // Save the headlines to the database
                headline.Id = await headlineRepository.CreateAssociatedPressHeadlineAsync(headline, job.Id);

                // Scrape the full article for each headline

            }

            // Log the scraping results
            LogScrapingResults(job);

            job.Success = true;
        }
        catch (Exception ex)
        {
            job.Success = false;
            job.ScrapeException = new ScrapeException() { Source = $"{nameof(Run)}", Exception = ex};
            throw;
        }
        finally
        {
            // Update ScrapeJob record with scrape results
            job.JobFinishedOn = DateTime.UtcNow;
            await scrapeJobRepository.UpdateScrapeJobAsync(job);
        }
    }

    private void LogScrapingResults(ScrapeJob job)
    {
        if (job.UseTestFile)
            logger.Log($"Scraping results from test file: {job.TestFile}");
        else
            logger.Log($"Scraping results from {job.SourceUri.AbsoluteUri}");

        if (job.PageScrapeResult is null)
        {
            logger.Log("\nThere is no Page Scrape Result!\n", LogLevel.Error);
            return;
        }

        logger.Log($"\nScraping Exceptions: {(job.PageScrapeResult.ScrapeExceptions.Count > 0 ? string.Empty : "None")}", logAsRawMessage: true);
        foreach (var exception in job.PageScrapeResult.ScrapeExceptions)
            logger.LogException(exception.Exception);

        logger.Log($"\nScraping Messages: {(job.PageScrapeResult.Messages.Count > 0 ? string.Empty : "None")}", logAsRawMessage: true);
        foreach (var message in job.PageScrapeResult.Messages)
            logger.Log($"Message from {message.Source}: {message.Message}", logAsRawMessage: true);
        
        logger.Log($"\n{job.PageScrapeResult.SectionsScraped} page sections found", logAsRawMessage: true);
        int headlineCount = 0;
        foreach (var headlineSection in job.PageScrapeResult.Headlines.DistinctBy(a => a.SectionName))
        {
            logger.Log($"{headlineSection.SectionName} Section", logAsRawMessage: true);
            var headlines = job.PageScrapeResult.Headlines.Where(a => a.SectionName == headlineSection.SectionName);
            foreach (var headline in headlines)
            {
                headlineCount++;
                logger.Log(headline.ToString(), logAsRawMessage: true);
            }
            logger.Log($"{headlines.Count()} headlines found in {headlineSection.SectionName}\n", logAsRawMessage: true);
        }
        logger.Log($"Total headlines found: {headlineCount}", logAsRawMessage: true);
        logger.Log($"Sections scraped: {job.PageScrapeResult.SectionsScraped}", logAsRawMessage: true);
        logger.Log($"Headlines scraped: {job.PageScrapeResult.HeadlinesScraped}", logAsRawMessage: true);
    }
}
