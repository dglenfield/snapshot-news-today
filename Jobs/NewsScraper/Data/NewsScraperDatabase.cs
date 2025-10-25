using Common.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using NewsScraper.Configuration.Options;

namespace NewsScraper.Data;

public class NewsScraperDatabase(IOptions<DatabaseOptions> options) : SqliteDatabase(options.Value.DatabaseFilePath)
{
    public async Task<bool> CreateAsync()
    {
        bool overwriteFlag = options.Value.DatabaseVersion.EndsWith("-overwrite", StringComparison.OrdinalIgnoreCase);
        if (File.Exists(DatabaseFilePath) && !overwriteFlag)
            return false; // Database file already exists, no need to create it again

        if (File.Exists(DatabaseFilePath) && overwriteFlag)
            await DeleteAsync(); // Delete existing database if overwrite flag is set

        await CreateDatabaseInfoTableAsync();
        await CreateScrapeAssociatedPressJobTableAsync();
        await CreateAssociatedPressHeadlineTableAsync();
        await CreateAssociatedPressArticleTableAsync();

        return true;
    }

    /// <summary>
    /// Asynchronously retrieves the current version string of the database.
    /// </summary>
    /// <returns>A string containing the database version. Throws an exception if the version cannot be determined.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the database version cannot be found.</exception>
    public async Task<string> GetDatabaseVersionAsync()
    {
        var result = await ExecuteScalarAsync("SELECT version FROM database_info LIMIT 1;");
        return result?.ToString() ?? throw new InvalidOperationException("Database version not found.");
    }

    private async Task CreateDatabaseInfoTableAsync()
    {
        await ExecuteNonQueryAsync(@"
                CREATE TABLE IF NOT EXISTS database_info (
                    version TEXT NOT NULL PRIMARY KEY, 
                    created_on TEXT NOT NULL DEFAULT (datetime('now')));");

        // Insert database_info record with database version
        string commandText = "PRAGMA foreign_keys = ON; INSERT INTO database_info (version) VALUES (@version);";
        SqliteParameter[] parameters = [new("@version", options.Value.DatabaseVersion.Split('-')[0])];
        int affectedRows = await ExecuteNonQueryAsync(commandText, parameters);
        if (affectedRows == 0)
            throw new InvalidOperationException("Insert database_info failed, no rows affected.");
    }

    #region Associated Press Table Creation Methods

    private async Task CreateScrapeAssociatedPressJobTableAsync()
    {
        string commandText = @"
                CREATE TABLE IF NOT EXISTS scrape_associated_press_job (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    source_name TEXT NOT NULL,
                    source TEXT NOT NULL,
                    job_started_on TEXT NOT NULL,
                    job_finished_on TEXT,
                    sections_scraped INTEGER,
                    headlines_scraped INTEGER,
                    articles_scraped INTEGER,
                    is_success INTEGER,
                    job_error TEXT,
                    main_page_errors TEXT,
                    articles_errors TEXT);";
        await ExecuteNonQueryAsync(commandText);
    }

    private async Task CreateAssociatedPressHeadlineTableAsync()
    {
        string commandText = @"
                CREATE TABLE IF NOT EXISTS associated_press_headline (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    job_id INTEGER, -- Foreign key to link to scrape_job table
                    section_name TEXT,
                    headline TEXT,
                    target_uri TEXT NOT NULL UNIQUE, -- target_uri is unique to prevent duplicate articles
                    last_updated_on TEXT,
                    published_on TEXT,
                    most_read INTEGER,
                    FOREIGN KEY(job_id) REFERENCES scrape_associated_press_job(id) ON DELETE CASCADE);";
        await ExecuteNonQueryAsync(commandText);
    }

    private async Task CreateAssociatedPressArticleTableAsync()
    {
        string commandText = @"
                CREATE TABLE IF NOT EXISTS associated_press_article (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    headline_id INTEGER, -- Foreign key to link to associated_press_headline table
                    scraped_on TEXT NOT NULL DEFAULT (datetime('now', 'utc')),
                    is_success INTEGER,
                    source TEXT NOT NULL,
                    headline TEXT,
                    author TEXT,
                    last_updated_on TEXT,
                    article_content TEXT,
                    error_message TEXT,
                    FOREIGN KEY(headline_id) REFERENCES associated_press_headline(id) ON DELETE CASCADE);";
        await ExecuteNonQueryAsync(commandText);
    }

    #endregion
}
