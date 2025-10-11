using Common.Data;
using Common.Logging;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace NewsScraper.Data.Providers;

public class ScraperDataProvider(string databaseFilePath, string databaseVersion, Logger logger) 
    : SqliteDataProvider(databaseFilePath)
{
    private readonly string _databaseVersion = string.IsNullOrWhiteSpace(databaseVersion) 
        ? throw new ArgumentNullException(nameof(databaseVersion)) : databaseVersion;
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
        await CreateSourceArticleTable();

        _logger.Log($"Database '{_fileName}' created successfully at '{_directoryPath}'.", LogLevel.Success);
    }

    public async Task<string> GetDatabaseVersionAsync()
    {
        return ExecuteScalarAsync("SELECT version FROM database_info LIMIT 1;").Result?.ToString() ?? 
            throw new InvalidOperationException("Database version not found.");
    }

    private async Task CreateDatabaseInfoTable()
    {
        try
        {
            // Create the database_info table if it doesn't exist
            await ExecuteNonQueryAsync(
                @"CREATE TABLE IF NOT EXISTS database_info (
                version TEXT NOT NULL PRIMARY KEY, 
                created_on TEXT NOT NULL DEFAULT (datetime('now')));");

            // Insert database_info record with database version
            string commandText = "PRAGMA foreign_keys = ON; INSERT INTO database_info (version) VALUES (@version);";
            SqliteParameter[] parameters = [new("@version", _databaseVersion.Split('-')[0])];
            int affectedRows = await this.ExecuteNonQueryAsync(commandText, parameters);
            if (affectedRows == 0)
                throw new InvalidOperationException("Insert database_info failed, no rows affected.");
        }
        catch (DbException)
        {
            _logger.Log($"Error creating the database_info table.", LogLevel.Error);
            throw;
        }
    }

    private async Task CreateScrapeJobRunTable()
    {
        try
        {
            // Create the scrape_job_run table if it doesn't exist
            string commandText =
                @"CREATE TABLE IF NOT EXISTS scrape_job_run (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_name TEXT NOT NULL,
                source_uri TEXT NOT NULL,
                source_articles_found INTEGER,
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

    private async Task CreateSourceArticleTable()
    {
        try
        {
            // Create the source_article table if it doesn't exist
            string commandText =
                @"CREATE TABLE IF NOT EXISTS source_article (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                job_run_id INTEGER, -- Foreign key to link to scrape_news_job_run table
                create_date TEXT NOT NULL DEFAULT (datetime('now')),
                source_name TEXT NOT NULL, 
                article_uri TEXT NOT NULL UNIQUE, -- article_uri is unique to prevent duplicate articles
                headline TEXT,
                publish_date TEXT,
                category TEXT,
                FOREIGN KEY(job_run_id) REFERENCES scrape_job_run(id) ON DELETE CASCADE);";
            await ExecuteNonQueryAsync(commandText);
        }
        catch (DbException)
        {
            _logger.Log($"Error creating the source_article table.", LogLevel.Error);
            throw;
        }
    }
}
