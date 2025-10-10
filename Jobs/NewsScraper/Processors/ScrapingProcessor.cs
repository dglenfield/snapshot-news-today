using Common.Logging;
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
internal class ScrapingProcessor(Logger logger, NewsProvider newsProvider, SqliteDataProvider sqliteDataProvider)
{
    public async Task Run()
    {
        NewsWebsite targetSite = NewsWebsite.CNN;
        ScrapeNewsJobRun scrapeNewsJobRun = new() { SourceName = targetSite.ToString(), SourceUri = new Uri("https://www.cnn.com") };

        try
        {
            // Insert initial ScrapeNewsJobRun record to track this scraping session
            scrapeNewsJobRun.Id = await sqliteDataProvider.InsertScrapeNewsJobRunAsync(scrapeNewsJobRun);

            // Get current news articles from CNN
            List<ArticleSource> sourceArticles = await newsProvider.GetNewsArticles(targetSite, scrapeNewsJobRun.Id);
            
            // Log retrieved articles
            logger.Log($"Total articles retrieved from {targetSite}: {sourceArticles.Count}", LogLevel.Debug);
            foreach (ArticleSource article in sourceArticles)
            {
                // Save each article to the database
                article.Id = await sqliteDataProvider.InsertArticleSourceAsync(article);
                logger.Log(article.ToString(), LogLevel.Debug, logAsRawMessage: true);
            }

            scrapeNewsJobRun.SourceArticlesFound = sourceArticles.Count;
            scrapeNewsJobRun.Success = true;
        }
        catch (Exception ex)
        {
            scrapeNewsJobRun.Success = false;
            scrapeNewsJobRun.ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            // Update ScrapeNewsJobRun record with scrape results
            scrapeNewsJobRun.ScrapeEnd = DateTime.UtcNow;
            await sqliteDataProvider.UpdateScrapeNewsJobRunAsync(scrapeNewsJobRun);
        }
    }
}
