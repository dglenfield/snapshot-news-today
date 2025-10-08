using Common.Logging;
using NewsScraper.Providers;

namespace NewsScraper.Processors;

// Workflow:
// Scrape news articles and save to local storage and Azure Cosmos DB
// 1. Get news articles from NewsProvider (e.g., CNN)
// 2. Get full article details for each article
// 3. Save articles to local storage
// 4. Save articles to Azure Cosmos DB

internal class NewsScraperProcessor(Logger logger, NewsProvider newsProvider)
{
    public void Run()
    {
        // Get current news articles from CNN
        NewsWebsite targetSite = NewsWebsite.CNN;
        var sourceArticles = newsProvider.GetNewsArticles(targetSite);
        if (sourceArticles.Count == 0)
        {
            logger.Log($"No articles found from {targetSite}.", LogLevel.Warning);
            return;
        }

        // Log retrieved articles
        if (Configuration.TestSettings.NewsProvider.GetNews.UseTestLandingPageFile)
        {
            logger.Log($"Total articles retrieved from {targetSite}: {sourceArticles.Count}");
            foreach (var article in sourceArticles)
                logger.Log(article.SourceUri.AbsoluteUri.ToString(), logAsRawMessage: true);
        }
    }
}