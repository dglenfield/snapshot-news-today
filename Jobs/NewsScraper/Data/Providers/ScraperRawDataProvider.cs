using Common.Data;
using Common.Logging;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace NewsScraper.Data.Providers;

internal class ScraperRawDataProvider(string databaseFilePath, string databaseVersion, Logger logger) 
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
        await CreateScrapeRawJobRunTable();

        _logger.Log($"Database '{_fileName}' created successfully at '{_directoryPath}'.", LogLevel.Success);
    }

    public async Task<string> GetDatabaseVersionAsync()
    {
        return ExecuteScalarAsync("SELECT version FROM database_info LIMIT 1;").Result?.ToString() 
            ?? throw new InvalidOperationException("Database version not found.");
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

    private async Task CreateScrapeRawJobRunTable()
    {
        try
        {
            // Create the scrape_raw_job_run table if it doesn't exist
            string commandText =
                @"CREATE TABLE IF NOT EXISTS scrape_raw_job_run (
                id INTEGER NOT NULL PRIMARY KEY,
                scraped_on TEXT NOT NULL DEFAULT (datetime('now')),
                raw_content TEXT);";
            await ExecuteNonQueryAsync(commandText);
        }
        catch (DbException)
        {
            _logger.Log($"Error creating the scrape_raw_job_run table.", LogLevel.Error);
            throw;
        }
    }
}
