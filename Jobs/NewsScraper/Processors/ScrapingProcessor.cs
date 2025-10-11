using Common.Logging;
using NewsScraper.Data;
using NewsScraper.Data.Providers;
using NewsScraper.Enums;
using NewsScraper.Models;
using NewsScraper.Providers;

namespace NewsScraper.Processors;

/// <summary>
/// Coordinates the workflow for scraping news articles from a specified provider and saving them to local storage and
/// Azure Cosmos DB.
/// </summary>
/// <remarks>This processor is designed to work with news sources such as CNN and handles both retrieval and
/// storage of articles. Logging is performed for diagnostic and auditing purposes, including details about the number
/// of articles retrieved and their content.</remarks>
/// <param name="logger">The logger instance used to record informational and warning messages during the scraping process.</param>
/// <param name="newsProvider">The news provider used to retrieve articles from the target news website.</param>
internal class ScrapingProcessor(Logger logger, NewsProvider newsProvider, ScrapeJobRunRepository scrapeJobRunRepository)
{
    public async Task Run()
    {
        NewsWebsite targetSite = NewsWebsite.CNN;
        ScrapeJobRun.SourceName = targetSite.ToString(); 
        ScrapeJobRun.SourceUri = new Uri("https://www.cnn.com");

        try
        {
            // Insert initial ScrapeJobRun record to track this scraping session
            ScrapeJobRun.Id = await scrapeJobRunRepository.CreateJobRunAsync();

            // Get current news articles from CNN
            List<SourceArticle> sourceArticles = await newsProvider.GetNewsArticles(targetSite);
            
            // Log retrieved articles
            logger.Log($"Total articles retrieved from {targetSite}: {sourceArticles.Count}", LogLevel.Debug);
            foreach (SourceArticle article in sourceArticles)
            {
                // Save each article to the database
                article.Id = await scrapeJobRunRepository.CreateSourceArticleAsync(article);
                logger.Log(article.ToString(), LogLevel.Debug, logAsRawMessage: true);
            }

            ScrapeJobRun.SourceArticlesFound = sourceArticles.Count;
            ScrapeJobRun.Success = true;
        }
        catch (Exception ex)
        {
            ScrapeJobRun.Success = false;
            ScrapeJobRun.ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            // Update ScrapeNewsJobRun record with scrape results
            ScrapeJobRun.ScrapeEnd = DateTime.UtcNow;
            await scrapeJobRunRepository.UpdateJobRunAsync();
        }
    }
}
