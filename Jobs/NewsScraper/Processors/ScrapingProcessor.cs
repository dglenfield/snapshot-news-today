using Common.Logging;
using NewsScraper.Data;
using NewsScraper.Enums;
using NewsScraper.Models;
using NewsScraper.Providers;
using NewsScraper.Scrapers.AssociatedPress;

namespace NewsScraper.Scrapers;

internal class ScrapingProcessor(Logger logger, CnnArticleProvider cnnArticleProvider, 
    MainPageScraper mainPageScraper,
    ScraperJobRunRepository scrapeJobRunRepository, NewsArticleRepository articleRepository)
{
    public async Task Run()
    {
        NewsWebsite targetSite = NewsWebsite.CNN;
        ScrapeJobRun.SourceName = targetSite.ToString(); 
        ScrapeJobRun.SourceUri = new Uri(Configuration.CnnBaseUrl);

        //await apArticleProvider.GetArticle();
        var scrapeResult = await mainPageScraper.Scrape();

        return;

        try
        {
            // Insert initial ScrapeJobRun record to track this scraping session
            ScrapeJobRun.Id = await scrapeJobRunRepository.CreateJobRunAsync();

            // Get current news articles from CNN
            List<Models.CNN.Article> newsArticles = await cnnArticleProvider.GetArticles();
            
            // Log retrieved news articles
            logger.Log($"Total news articles retrieved from {targetSite}: {newsArticles.Count}", LogLevel.Debug);
            foreach (Models.CNN.Article article in newsArticles)
            {
                // Save each article to the database
                article.Id = await articleRepository.CreateNewsArticleAsync(article);
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
                if (!await articleRepository.UpdateNewsArticleAsync(article))
                    logger.Log($"Failed to update article content for story ID {article.Id}", LogLevel.Warning);
                else
                    logger.Log(article.ToString(), LogLevel.Debug, logAsRawMessage: true);
                break; // TEMPORARY: Process only the first article for testing
            }

            ScrapeJobRun.NewsArticlesFound = newsArticles.Count;
            ScrapeJobRun.NewsArticlesScraped = newsArticles.Count(a => a.Success == true);
            ScrapeJobRun.Success = true;
        }
        catch (Exception ex)
        {
            ScrapeJobRun.Success = false;
            ScrapeJobRun.ErrorMessage = [ex.Message];
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
