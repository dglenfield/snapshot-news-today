using Common.Logging;
using NewsScraper.Data;
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
/// <param name="newsStoryProvider">The news provider used to retrieve articles from the target news website.</param>
internal class ScrapingProcessor(Logger logger, NewsStoryProvider newsStoryProvider, 
    NewsArticleProvider articleProvider, ScraperJobRunRepository scrapeJobRunRepository, 
    NewsArticleRepository articleRepository)
{
    public async Task Run()
    {
        NewsWebsite targetSite = NewsWebsite.CNN;
        ScrapeJobRun.SourceName = targetSite.ToString(); 
        ScrapeJobRun.SourceUri = new Uri(Configuration.CnnBaseUrl);

        try
        {
            // Insert initial ScrapeJobRun record to track this scraping session
            ScrapeJobRun.Id = await scrapeJobRunRepository.CreateJobRunAsync();

            // Get current news articles from CNN
            List<SourceArticle> newsArticles = await newsStoryProvider.GetNewsStories(targetSite);
            
            // Log retrieved news articles
            logger.Log($"Total news articles retrieved from {targetSite}: {newsArticles.Count}", LogLevel.Debug);
            foreach (SourceArticle newsStory in newsArticles)
            {
                // Save each article to the database
                newsStory.Id = await articleRepository.CreateNewsArticleAsync(newsStory);
                logger.Log(newsStory.ToString(), LogLevel.Debug, logAsRawMessage: true);
            }

            foreach (SourceArticle newsStory in newsArticles)
            {
                if (newsStory.ArticleUri is null)
                {
                    logger.Log($"Skipping article with missing URI for story ID {newsStory.Id}", LogLevel.Warning);
                    continue;
                }

                // Scrape and save the full article content
                await articleProvider.GetArticle(newsStory);
                if (!await articleRepository.UpdateNewsArticleAsync(newsStory))
                    logger.Log($"Failed to update article content for story ID {newsStory.Id}", LogLevel.Warning);
                else
                    logger.Log(newsStory.ToString(), LogLevel.Debug, logAsRawMessage: true);
                break; // TEMPORARY: Process only the first article for testing
            }

            ScrapeJobRun.NewsStoriesFound = newsArticles.Count;
            ScrapeJobRun.NewsArticlesScraped = newsArticles.Count(a => a.Success == true);
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
