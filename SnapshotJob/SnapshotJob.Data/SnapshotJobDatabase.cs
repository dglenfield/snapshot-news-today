using Microsoft.Extensions.Options;
using SnapshotJob.Common.Logging;
using SnapshotJob.Data.Configuration.Options;
using SnapshotJob.Data.Models;
using SnapshotJob.Data.Repositories;

namespace SnapshotJob.Data;

public class SnapshotJobDatabase(IOptions<DatabaseOptions> options, Logger logger) 
    : SqliteDatabase(options.Value.DatabaseFilePath)
{   
    public async Task InitializeAsync()
    {
        if (options.Value.DeleteExistingDatabase)
        {
            await DeleteAsync();
            logger.Log($"Database deleted at '{DatabaseFilePath}'.", LogLevel.Success);
        }

        DatabaseInfoRepository databaseInfo = new(this);
        TopStoryApiCallRepository topStoryApiCall = new(this);
        if (!File.Exists(DatabaseFilePath))
        {
            // Create database_info table
            await databaseInfo.CreateTableAsync();

            // Create news_snapshot table
            NewsSnapshotRepository newsSnapshot = new(this);
            await newsSnapshot.CreateTableAsync();

            // Create scraped_headline table
            ScrapedHeadlineRepository headlineScrapeRepository = new(this);
            await headlineScrapeRepository.CreateTableAsync();

            // Create scraped_article table
            ScrapedArticleRepository apNewsArticle = new(this);
            await apNewsArticle.CreateTableAsync();

            // Create top_story_api_call table
            await topStoryApiCall.CreateTableAsync();

            // Create top_story table
            TopStoryRepository topStory = new(this);
            await topStory.CreateTableAsync();

            logger.Log($"Database created at '{DatabaseFilePath}'.", LogLevel.Success);

            return;
        }

        // Updates for news_snapshot table (example of how to use in the future)
        var newsSnapshotDbInfo = await databaseInfo.GetAsync("news_snapshot");
        if (newsSnapshotDbInfo.Version != "1.1")
        {
            logger.Log("UPDATE TABLE");
        }
        else
        {
            logger.Log("TABLE UP TO DATE");
        }

        // Updates for top_story_api_call table
        var topStoryApiCallDbInfo = await databaseInfo.GetAsync("top_story_api_call");
        if (topStoryApiCallDbInfo is null)
        {
            await topStoryApiCall.CreateTableAsync();
            logger.Log($"top_story_api_call table created at '{DatabaseFilePath}'.", LogLevel.Success);
        }
    }
}
