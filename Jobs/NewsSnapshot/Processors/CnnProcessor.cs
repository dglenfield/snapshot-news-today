using Common.Data.Repositories;
using Common.Logging;
using Common.Models.CNN;
using NewsSnapshot.Scrapers.CNN;

namespace NewsSnapshot.Processors;

internal class CnnProcessor(Logger logger, CnnArticleProvider cnnArticleProvider, APNewsArticleRepository articleRepository)
{
    internal async Task Run()
    {
        // Get current news articles from CNN
        List<Article> newsArticles = await cnnArticleProvider.GetArticles();

        // Log retrieved news articles
        //logger.Log($"Total news articles retrieved from {ScrapeJob.SourceName}: {newsArticles.Count}", LogLevel.Debug);
        foreach (Article article in newsArticles)
        {
            // Save each article to the database
            //article.Id = await articleRepository.CreateAsync(article);
            logger.Log(article.ToString(), LogLevel.Debug, logAsRawMessage: true);
        }

        foreach (Article article in newsArticles)
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
