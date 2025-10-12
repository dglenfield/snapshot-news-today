using Common.Logging;
using System.Data.Common;

namespace NewsScraper.Data.Providers;

/// <summary>
/// Provides data access and management functionality for the scraper job, including database creation and schema
/// setup for storing job runs and news story articles.
/// </summary>
/// <remarks>This provider is responsible for initializing the database and creating required tables for tracking
/// scrape job runs and associated news story articles. If the database version ends with '-overwrite', any existing
/// database at the specified path will be deleted and recreated. All operations are logged using the provided logger.
/// </remarks>
/// <param name="databaseFilePath">The file path to the SQLite database used for storing scraper job data. Must be a valid path accessible for read and
/// write operations.</param>
/// <param name="databaseVersion">The version identifier for the database schema. Used to determine schema setup and overwrite behavior.</param>
/// <param name="logger">The logger instance used to record informational and error messages during database operations.</param>
public class ScraperJobDataProvider(string databaseFilePath, string databaseVersion, Logger logger) 
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
        await CreateScrapeJobRunTable();
        await CreateNewsStoryArticleTable();
        _logger.Log($"Database '{_fileName}' created successfully at '{_directoryPath}'.", LogLevel.Success);
    }

    #region Table Creation Methods

    private async Task CreateScrapeJobRunTable()
    {
        try
        {
            // Create the scrape_job_run table if it doesn't exist
            string commandText = @"
                CREATE TABLE IF NOT EXISTS scrape_job_run (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    source_name TEXT NOT NULL,
                    source_uri TEXT NOT NULL,
                    news_stories_found INTEGER,
                    news_articles_scraped INTEGER,
                    scrape_start TEXT NOT NULL DEFAULT (datetime('now')),
                    scrape_end TEXT,
                    success INTEGER,
                    error_message TEXT);";
            await ExecuteNonQueryAsync(commandText);
        }
        catch (DbException)
        {
            _logger.Log($"Error creating the scrape_job_run table.", LogLevel.Error);
            throw;
        }
    }

    private async Task CreateNewsStoryArticleTable()
    {
        try
        {
            // Create the news_story_article table if it doesn't exist
            string commandText = @"
                CREATE TABLE IF NOT EXISTS news_story_article (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    job_run_id INTEGER, -- Foreign key to link to scrape_news_job_run table
                    create_date TEXT NOT NULL DEFAULT (datetime('now')),
                    source_name TEXT NOT NULL, 
                    article_uri TEXT NOT NULL UNIQUE, -- article_uri is unique to prevent duplicate articles
                    category TEXT,
                    article_headline TEXT,
                    story_headline TEXT,
                    author TEXT,
                    original_publish_date TEXT,
                    last_updated_date TEXT,
                    is_paywalled INTEGER,
                    article_content TEXT,
                    success INTEGER,
                    error_message TEXT,
                    FOREIGN KEY(job_run_id) REFERENCES scrape_job_run(id) ON DELETE CASCADE);";
            await ExecuteNonQueryAsync(commandText);
        }
        catch (DbException)
        {
            _logger.Log($"Error creating the news_story_article table.", LogLevel.Error);
            throw;
        }
    }

    #endregion
}
