using Common.Logging;
using Microsoft.Data.Sqlite;
using NewsScraper.Models;
using System.Data.Common;

namespace NewsScraper.Providers;

public class SqliteDataProvider(string databaseFilePath, string databaseVersion, Logger logger)
{
    public string DatabaseFilePath => _databaseFilePath;

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

        // Create the scrape_news_job_run table if it doesn't exist
        await using var createScrapeNewsJobRunTableCommand = connection.CreateCommand();
        createScrapeNewsJobRunTableCommand.CommandText =
            @"CREATE TABLE IF NOT EXISTS scrape_news_job_run (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_name TEXT NOT NULL,
                source_uri TEXT NOT NULL, 
                scrape_start TEXT NOT NULL DEFAULT (datetime('now')),
                scrape_end TEXT,
                raw_output TEXT
            );";
        await createScrapeNewsJobRunTableCommand.ExecuteNonQueryAsync();

        // ArticleSource -> SourceArticle
        // Scrape for article sources, then scrape each article source for full article details (SourceArticle)

        // Create the scrape_found_article_source table if it doesn't exist
        await using var createArticleSourceTableCommand = connection.CreateCommand();
        createArticleSourceTableCommand.CommandText =
            @"CREATE TABLE IF NOT EXISTS article_source (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                job_run_id INTEGER, -- Foreign key to link to scrape_news_job_run table
                create_date TEXT NOT NULL DEFAULT (datetime('now')),
                source_name TEXT NOT NULL, 
                article_uri TEXT NOT NULL UNIQUE, -- article_uri is unique to prevent duplicate articles
                headline TEXT,
                publish_date TEXT,
                category TEXT,
                FOREIGN KEY(job_run_id) REFERENCES scrape_news_job_run(id) ON DELETE CASCADE
            );";
        await createArticleSourceTableCommand.ExecuteNonQueryAsync();
        
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

    public async Task UpdateScrapeNewsJobRunAsync(ScrapeNewsJobRun scrapeNewsJobRun)
    {
        if (!File.Exists(_databaseFilePath))
            throw new FileNotFoundException($"Database file not found: {_databaseFilePath}");
        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync();
        
        await using var command = connection.CreateCommand();
        command.CommandText =
            @"UPDATE scrape_news_job_run
            SET scrape_end = @scrape_end
            WHERE id = @id;";
        command.Parameters.AddWithValue("@scrape_end", (object?)scrapeNewsJobRun.ScrapeEnd?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value);
        command.Parameters.AddWithValue("@id", scrapeNewsJobRun.Id);

        try
        {
            int rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
                throw new InvalidOperationException($"No record found with id {scrapeNewsJobRun.Id} to update.");
        }
        catch (Exception)
        {
            _logger.Log("Error updating record in scraped_news_source.", LogLevel.Error);
            throw;
        }
    }

    public async Task<long> InsertScrapeNewsJobRunAsync(ScrapeNewsJobRun scrapeNewsJobRun)
    {
        if (!File.Exists(_databaseFilePath))
            throw new FileNotFoundException($"Database file not found: {_databaseFilePath}");

        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync();
        
        await using var command = connection.CreateCommand();
        command.CommandText =
            @"INSERT INTO scrape_news_job_run (source_name, source_uri, scrape_end, raw_output)
            VALUES (@source_name, @source_uri, @scrape_end, @raw_output);";
        command.Parameters.AddWithValue("@source_name", (object)scrapeNewsJobRun.SourceName);
        command.Parameters.AddWithValue("@source_uri", (object)scrapeNewsJobRun.SourceUri.AbsoluteUri);
        command.Parameters.AddWithValue("@scrape_end", (object?)scrapeNewsJobRun.ScrapeEnd?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value);
        command.Parameters.AddWithValue("@raw_output", (object?)scrapeNewsJobRun.RawOutput ?? DBNull.Value);

        try
        {
            await command.ExecuteNonQueryAsync();
            // Get the auto-incremented row ID
            await using var idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid();";
            object? result = await idCommand.ExecuteScalarAsync();
            if (result is long insertedId)
                return insertedId;
            if (result is int intId)
                return intId;
        }
        catch (Exception)
        {
            _logger.Log("Error inserting record into scraped_news_source.", LogLevel.Error);
            throw;
        }
        throw new InvalidOperationException("Insert scrape_news_job_run failed, no row id returned.");
    }

    public async Task<long> InsertArticleSourceAsync(NewsArticle sourceArticle)
    {
        if (!File.Exists(_databaseFilePath))
            throw new FileNotFoundException($"Database file not found: {_databaseFilePath}");

        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync();
        
        await using var command = connection.CreateCommand();
        command.CommandText =
            @"INSERT INTO article_source (job_run_id, source_name, article_uri, headline, publish_date, category)
            VALUES (@job_run_id, @source_name, @article_uri, @headline, @publish_date, @category);";
        command.Parameters.AddWithValue("@job_run_id", (object?)sourceArticle.JobRunId ?? DBNull.Value); // Placeholder
        command.Parameters.AddWithValue("@source_name", (object?)sourceArticle.SourceName ?? DBNull.Value); // Placeholder
        command.Parameters.AddWithValue("@article_uri", (object?)sourceArticle.SourceUri.AbsoluteUri ?? DBNull.Value); // Placeholder
        command.Parameters.AddWithValue("@headline", (object?)sourceArticle.SourceHeadline ?? DBNull.Value); // Placeholder
        command.Parameters.AddWithValue("@publish_date", (object?)sourceArticle.SourcePublishDate ?? DBNull.Value); // Placeholder
        command.Parameters.AddWithValue("@category", (object?)sourceArticle.SourceCategory ?? DBNull.Value); // Placeholder

        try
        {
            await command.ExecuteNonQueryAsync();
            // Get the auto-incremented row ID
            await using var idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid();";
            object? result = await idCommand.ExecuteScalarAsync();
            if (result is long insertedId)
                return insertedId;
            if (result is int intId)
                return intId;
        }
        catch (Exception)
        {
            _logger.Log("Error inserting record into article_source.", LogLevel.Error);
            throw;
        }
        throw new InvalidOperationException("Insert article_source failed, no row id returned.");
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
