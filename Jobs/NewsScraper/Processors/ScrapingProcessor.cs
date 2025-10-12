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
    NewsArticleProvider articleProvider, ScrapeJobRunRepository scrapeJobRunRepository)
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

            // Get current news stories from CNN
            List<SourceNewsStory> newsStories = await newsStoryProvider.GetNewsStories(targetSite);
            
            // Log retrieved news stories
            logger.Log($"Total news stories retrieved from {targetSite}: {newsStories.Count}", LogLevel.Debug);
            foreach (SourceNewsStory newsStory in newsStories)
            {
                // Save each article to the database
                newsStory.Id = await scrapeJobRunRepository.CreateNewsStoryArticleAsync(newsStory);
                logger.Log(newsStory.ToString(), LogLevel.Debug, logAsRawMessage: true);
            }

            foreach (SourceNewsStory newsStory in newsStories)
            {
                if (newsStory.Article?.ArticleUri is null)
                {
                    logger.Log($"Skipping article with missing URI for story ID {newsStory.Id}", LogLevel.Warning);
                    continue;
                }

                // Scrape and save the full article content
                newsStory.Article = await articleProvider.GetArticle(newsStory.Article.ArticleUri);
                logger.Log(newsStory.ToString(), LogLevel.Debug, logAsRawMessage: true);
                break; // TEMPORARY: Process only the first article for testing
            }

            ScrapeJobRun.NewsStoriesFound = newsStories.Count;
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
