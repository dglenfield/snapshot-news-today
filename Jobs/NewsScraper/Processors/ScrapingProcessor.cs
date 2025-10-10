using Common.Logging;
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
        // Get current news articles from CNN
        NewsWebsite targetSite = NewsWebsite.CNN;
        // Insert a ScrapeNewsJobRun record
        ScrapeNewsJobRun scrapeNewsJobRun = new() { SourceName = targetSite.ToString(), SourceUri = new Uri("https://www.cnn.com") };
        scrapeNewsJobRun.Id = await sqliteDataProvider.InsertScrapeNewsJobRunAsync(scrapeNewsJobRun);
        List<NewsArticle> sourceArticles = await newsProvider.GetNewsArticles(targetSite, scrapeNewsJobRun.Id);
        if (sourceArticles.Count == 0)
        {
            logger.Log($"No articles found from {targetSite}.", LogLevel.Warning);
            return;
        }

        // Log retrieved articles
        logger.Log($"Total articles retrieved from {targetSite}: {sourceArticles.Count}", LogLevel.Debug);
        foreach (NewsArticle article in sourceArticles)
        {
            // Save each article to the database
            article.Id = await sqliteDataProvider.InsertArticleSourceAsync(article); 
            logger.Log(article.ToString(), LogLevel.Debug, logAsRawMessage: true);
        }

        // Update ScrapeNewsJobRun record with results
        scrapeNewsJobRun.ScrapeEnd = DateTime.UtcNow;
        await sqliteDataProvider.UpdateScrapeNewsJobRunAsync(scrapeNewsJobRun);
    }
}
