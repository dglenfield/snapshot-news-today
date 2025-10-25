using Common.Logging;
using NewsScraper.Data.Repositories;
using NewsScraper.Models;
using NewsScraper.Providers;

namespace NewsScraper.Processors;

internal class CnnProcessor(Logger logger, CnnArticleProvider cnnArticleProvider, AssociatedPressArticleRepository articleRepository)
{
    internal async Task Run()
    {
        // Get current news articles from CNN
        List<Models.CNN.Article> newsArticles = await cnnArticleProvider.GetArticles();

        // Log retrieved news articles
        //logger.Log($"Total news articles retrieved from {ScrapeJob.SourceName}: {newsArticles.Count}", LogLevel.Debug);
        foreach (Models.CNN.Article article in newsArticles)
        {
            // Save each article to the database
            //article.Id = await articleRepository.CreateAsync(article);
            logger.Log(article.ToString(), LogLevel.Debug, logAsRawMessage: true);
        }

        foreach (Models.CNN.Article article in newsArticles)
        {
            if (article.ArticleUri is null)
            {
                logger.Log($"Skipping article with missing URI for story ID {article.Id}", LogLevel.Warning);
                continue;
            }

            // Scrape and save the full article content
            await cnnArticleProvider.GetArticle(article);
            //if (!await articleRepository.UpdateArticleAsync(article))
            //    logger.Log($"Failed to update article content for story ID {article.Id}", LogLevel.Warning);
            //else
            //    logger.Log(article.ToString(), LogLevel.Debug, logAsRawMessage: true);
            break; // TEMPORARY: Process only the first article for testing
        }
    }
}
