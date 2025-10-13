using Common.Logging;
using System.Data.Common;

namespace NewsScraper.Data.Providers;

/// <summary>
/// Provides raw data storage for scraper job runs.
/// </summary>
/// <param name="databaseFilePath">The file path to the database where scraper job data will be stored and accessed. Must be a valid path to a writable
/// location.</param>
/// <param name="databaseVersion">The version identifier for the database schema. May include the '-overwrite' suffix to indicate that the database
/// should be recreated if it already exists.</param>
/// <param name="logger">The logger instance used to record operational events and errors during database operations. Cannot be null.</param>
internal class ScraperJobRawDataProvider(string databaseFilePath, string databaseVersion, Logger logger)
    : BaseDataProvider(databaseFilePath, databaseVersion, logger)
{
    private readonly Logger _logger = logger;

    public async Task CreateDatabaseAsync()
    {
        bool overwriteFlag = _databaseVersion.EndsWith("-overwrite", StringComparison.OrdinalIgnoreCase);
        if (File.Exists(_databaseFilePath) && !overwriteFlag)
            return; // Database file already exists, no need to create it again
        else if (overwriteFlag)
            await DeleteAsync(); // Delete existing database if overwrite flag is set

        await CreateDatabaseInfoTable();
        await CreateNewsSourceScrapeTable();
        await CreateNewsArticleScrapeTable();
        _logger.Log($"Database '{_fileName}' created successfully at '{_directoryPath}'.", LogLevel.Success);
    }

    #region Table Creation

    private async Task CreateNewsSourceScrapeTable()
    {
        try
        {
            // Create the news_source_scrape table if it doesn't exist
            string commandText = @"
                CREATE TABLE IF NOT EXISTS news_source_scrape (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    scrape_job_run_id INTEGER NOT NULL,
                    scraped_on TEXT NOT NULL DEFAULT (datetime('now')),
                    raw_content TEXT);";
            await ExecuteNonQueryAsync(commandText);
        }
        catch (DbException)
        {
            _logger.Log($"Error creating the news_source_scrape table.", LogLevel.Error);
            throw;
        }
    }

    private async Task CreateNewsArticleScrapeTable()
    {
        try
        {
            // Create the news_article_scrape table if it doesn't exist
            string commandText = @"
                CREATE TABLE IF NOT EXISTS news_article_scrape (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    scrape_job_run_id INTEGER NOT NULL,
                    news_story_article_id INTEGER NOT NULL,
                    scraped_on TEXT NOT NULL DEFAULT (datetime('now')),
                    article_uri TEXT NOT NULL,
                    raw_content TEXT);";
            await ExecuteNonQueryAsync(commandText);
        }
        catch (DbException)
        {
            _logger.Log($"Error creating the news_article_scrape table.", LogLevel.Error);
            throw;
        }
    }

    #endregion
}
