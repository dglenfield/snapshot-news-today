using Common.Logging;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace NewsScraper.Providers;

public class SqliteDataProvider(string databaseFilePath, string databaseVersion, Logger logger)
{
    private readonly string _connectionString = $"Data Source={databaseFilePath};Pooling=False";
    private readonly string _databaseFilePath = string.IsNullOrWhiteSpace(databaseFilePath) ?
            throw new ArgumentNullException(nameof(databaseFilePath)) : databaseFilePath;
    private readonly string _databaseVersion = string.IsNullOrWhiteSpace(databaseVersion) ?
            throw new ArgumentNullException(nameof(databaseVersion)) : databaseVersion;
    private readonly string _directoryPath = Path.GetDirectoryName(databaseFilePath) ?? throw new DirectoryNotFoundException("Directory path missing or invalid.");
    private readonly string _fileName = Path.GetFileName(databaseFilePath) ?? throw new DirectoryNotFoundException("File name missing or invalid.");
    private readonly Logger _logger = logger;

    public async Task CreateAsync()
    {
        bool overwriteFlag = _databaseVersion.EndsWith("-overwrite", StringComparison.OrdinalIgnoreCase);
        if (File.Exists(_databaseFilePath) && !overwriteFlag)
            return; // Database file already exists, no need to create it again
        else if (overwriteFlag)
            await DeleteAsync(); // Delete existing database if overwrite flag is set

        // Ensure the directory exists or create it
        if (!Directory.Exists(_directoryPath))
            Directory.CreateDirectory(_directoryPath);

        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync();

        // Create the database_info table if it doesn't exist
        try
        {
            await using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"CREATE TABLE IF NOT EXISTS database_info (version TEXT NOT NULL PRIMARY KEY);";
            await createTableCommand.ExecuteNonQueryAsync();
        }
        catch (DbException)
        {
            _logger.Log($"Error creating the database_info table.", LogLevel.Error);
            throw;
        }

        // Insert version using a parameter
        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "PRAGMA foreign_keys = ON; INSERT INTO database_info (version) VALUES (@version);";
        insertCommand.Parameters.AddWithValue("@version", _databaseVersion.Split('-')[0]);
        await insertCommand.ExecuteNonQueryAsync();

        // Create the scraped_news_source table if it doesn't exist
        await using var createScrapedNewsSourceTableCommand = connection.CreateCommand();
        createScrapedNewsSourceTableCommand.CommandText =
            @"CREATE TABLE IF NOT EXISTS scraped_news_source (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_uri TEXT NOT NULL, 
                scrape_datetime TEXT NOT NULL DEFAULT (datetime('now')),
                raw_output TEXT NOT NULL
            );";
        await createScrapedNewsSourceTableCommand.ExecuteNonQueryAsync();

        // Create the source_article table if it doesn't exist
        await using var createSourceArticleTableCommand = connection.CreateCommand();
        createSourceArticleTableCommand.CommandText =
            @"CREATE TABLE IF NOT EXISTS source_article (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_id INTEGER, -- Foreign key to link to scraped_news_source table
                create_date TEXT NOT NULL DEFAULT (datetime('now')),
                source_name TEXT NOT NULL, 
                article_uri TEXT NOT NULL UNIQUE, -- article_uri is unique to prevent duplicate articles
                headline TEXT,
                publish_date TEXT,
                category TEXT,
                FOREIGN KEY(source_id) REFERENCES scraped_news_source(id) ON DELETE CASCADE
            );";
        await createSourceArticleTableCommand.ExecuteNonQueryAsync();
        
        _logger.Log($"Database '{_fileName}' created successfully at '{_directoryPath}'.", LogLevel.Success);
    }

    public async Task DeleteAsync()
    {
        if (File.Exists(_databaseFilePath))
        {
            await Task.Delay(100); // Give the OS a moment to release any file lock
            try
            {
                File.Delete(_databaseFilePath);
                _logger.Log($"Database file '{_databaseFilePath}' deleted successfully.", LogLevel.Success);
            }
            catch (Exception ex)
            {
                _logger.Log($"Error deleting database file '{_databaseFilePath}'", LogLevel.Error);
                _logger.LogException(ex);
                throw;
            }
        }
    }

    public async Task<string?> GetDatabaseVersionAsync()
    {
        if (!File.Exists(_databaseFilePath))
            throw new FileNotFoundException($"Database file not found: {_databaseFilePath}");
        
        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync();
        
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT version FROM database_info LIMIT 1;";

        await using var reader = command.ExecuteReaderAsync().Result;
        if (await reader.ReadAsync())
            return reader.GetString(0);

        return null; // No version found
    }
}
