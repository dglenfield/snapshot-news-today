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
        await CreateScrapeJobTable();
        await CreateAssociatedPressHeadlineTable();
        await CreateNewsArticleTable();
        _logger.Log($"Database '{_fileName}' created successfully at '{_directoryPath}'.", LogLevel.Success);
    }

    #region Table Creation Methods

    private async Task CreateScrapeJobTable()
    {
        try
        {
            // Create the scrape_job_run table if it doesn't exist
            string commandText = @"
                CREATE TABLE IF NOT EXISTS scrape_job (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    source_name TEXT NOT NULL,
                    source_uri TEXT NOT NULL,
                    job_started_on TEXT NOT NULL,
                    job_finished_on TEXT,
                    sections_scraped INTEGER,
                    headlines_scraped INTEGER,
                    scrape_success INTEGER,
                    error_messages TEXT);";
            await ExecuteNonQueryAsync(commandText);
        }
        catch (DbException)
        {
            _logger.Log($"Error creating the scrape_job table.", LogLevel.Error);
            throw;
        }
    }

    private async Task CreateAssociatedPressHeadlineTable()
    {
        try
        {
            // Create the associated_press_headline table if it doesn't exist
            string commandText = @"
                CREATE TABLE IF NOT EXISTS associated_press_headline (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    scrape_job_id INTEGER, -- Foreign key to link to scrape_job table
                    section_name TEXT,
                    headline TEXT,
                    target_uri TEXT NOT NULL UNIQUE, -- target_uri is unique to prevent duplicate articles
                    last_updated_on TEXT,
                    published_on TEXT,
                    most_read INTEGER,
                    FOREIGN KEY(scrape_job_id) REFERENCES scrape_job(id) ON DELETE CASCADE);";
            await ExecuteNonQueryAsync(commandText);
        }
        catch (DbException)
        {
            _logger.Log($"Error creating associated_press_headline table.", LogLevel.Error);
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
                    article_headline_id INTEGER, -- Foreign key to link to article_headline table
                    create_date TEXT NOT NULL DEFAULT (datetime('now', 'utc')),
                    article_uri TEXT NOT NULL UNIQUE, -- article_uri is unique to prevent duplicate articles
                    category TEXT,
                    headline TEXT,
                    author TEXT,
                    original_publish_date TEXT,
                    last_updated_date TEXT,
                    article_content TEXT,
                    success INTEGER,
                    error_message TEXT,
                    FOREIGN KEY(article_headline_id) REFERENCES ap_news_article_headline(id) ON DELETE CASCADE);";
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
