using Common.Logging;
using Microsoft.Extensions.Options;
using NewsScraper.Configuration.Options;
using System.Data.Common;

namespace NewsScraper.Data.Providers;

public class ScrapeJobDataProvider(Logger logger, IOptions<DatabaseOptions> databaseOptions)
    : BaseDataProvider(logger, databaseOptions)
{
    private readonly DatabaseOptions _databaseOptions = databaseOptions.Value;
    private readonly Logger _logger = logger;

    public async Task CreateDatabaseAsync()
    {
        bool overwriteFlag = _databaseOptions.NewsScraperJob.DatabaseVersion.EndsWith("-overwrite", StringComparison.OrdinalIgnoreCase);
        if (File.Exists(_databaseFilePath) && !overwriteFlag)
            return; // Database file already exists, no need to create it again
        else if (overwriteFlag)
            await DeleteAsync(); // Delete existing database if overwrite flag is set

        await CreateDatabaseInfoTable();
        await CreateScrapeJobRunTable();
        await CreateNewsArticleTable();
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
                    sections_scraped INTEGER,
                    articles_scraped INTEGER,
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

    private async Task CreateNewsArticleTable()
    {
        try
        {
            // Create the news_article table if it doesn't exist
            string commandText = @"
                CREATE TABLE IF NOT EXISTS news_article (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    job_run_id INTEGER, -- Foreign key to link to scrape_news_job_run table
                    create_date TEXT NOT NULL DEFAULT (datetime('now')),
                    source_name TEXT NOT NULL, 
                    article_uri TEXT NOT NULL UNIQUE, -- article_uri is unique to prevent duplicate articles
                    category TEXT,
                    headline TEXT,
                    story_headline TEXT,
                    author TEXT,
                    original_publish_date TEXT,
                    last_updated_date TEXT,
                    article_content TEXT,
                    success INTEGER,
                    error_message TEXT,
                    FOREIGN KEY(job_run_id) REFERENCES scrape_job_run(id) ON DELETE CASCADE);";
            await ExecuteNonQueryAsync(commandText);
        }
        catch (DbException)
        {
            _logger.Log($"Error creating the news_article table.", LogLevel.Error);
            throw;
        }
    }

    #endregion
}
