using Common.Configuration.Options;
using Common.Data.Repositories;
using Common.Logging;
using Microsoft.Extensions.Options;

namespace Common.Data;

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

        if (!File.Exists(DatabaseFilePath))
        {
            // Create database_info table
            DatabaseInfoRepository databaseInfo = new(this);
            await databaseInfo.CreateTableAsync();

            // Create snapshot_news_job table
            NewsSnapshotRepository newsSnapshot = new(this);
            await newsSnapshot.CreateTableAsync();

            // Create scraped_headline table
            ScrapedHeadlineRepository headlineScrapeRepository = new(this);
            await headlineScrapeRepository.CreateTableAsync();

            // Create scraped_article table
            ScrapedArticleRepository apNewsArticle = new(this);
            await apNewsArticle.CreateTableAsync();

            // Create news_analysis table
            NewsAnalysisRepository newsAnalysis = new(this);
            await newsAnalysis.CreateTableAsync();

            logger.Log($"Database created at '{DatabaseFilePath}'.", LogLevel.Success);
        }
    }
}
