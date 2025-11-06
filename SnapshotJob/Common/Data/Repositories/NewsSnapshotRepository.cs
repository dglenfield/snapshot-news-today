using Common.Models;
using Microsoft.Data.Sqlite;

namespace Common.Data.Repositories;

public class NewsSnapshotRepository(SnapshotJobDatabase database)
{
    public async Task<long> CreateAsync(NewsSnapshot snapshot)
    {
        string commandText = @"
            INSERT INTO news_snapshot (started_on) 
            VALUES (@started_on);";

        SqliteParameter[] parameters = [
            new("@started_on", (object?)snapshot.StartedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value)
        ];

        long id = await database.InsertAsync(commandText, parameters);
        if (id > 0)
            return id;

        throw new InvalidOperationException("Insert into news_snapshot failed, no row id returned.");
    }

    public async Task CreateTableAsync()
    {
        string commandText = @"
            CREATE TABLE IF NOT EXISTS news_snapshot (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                started_on TEXT NOT NULL,
                finished_on TEXT,
                run_time_in_seconds INTEGER,
                sections_scraped INTEGER,
                headlines_scraped INTEGER,
                articles_scraped INTEGER,
                is_success INTEGER,
                job_error TEXT,
                scrape_errors TEXT);

            INSERT INTO database_info (entity, version) 
                VALUES ('news_snapshot', '1.1');";

        await database.ExecuteNonQueryAsync(commandText);
    }

    public async Task UpdateAsync(NewsSnapshot snapshot)
    {
        string jobError = snapshot.JobException is not null 
            ? $"{snapshot.JobException.Source}: {snapshot.JobException.Message}" : string.Empty;

        string scrapeErrors = string.Empty;
        if (snapshot.ScrapeHeadlinesResult?.ScrapeExceptions is not null)
            foreach (var exception in snapshot.ScrapeHeadlinesResult.ScrapeExceptions)
                scrapeErrors += $"{exception.Source}: {exception.Message} | ";
        if (snapshot.ScrapeArticlesResult?.ScrapeException is not null)
            scrapeErrors += $"{snapshot.ScrapeArticlesResult.ScrapeException.Source}: {snapshot.ScrapeArticlesResult.ScrapeException.Message} | ";
        if (snapshot.ScrapeArticlesResult?.ScrapedArticles is not null)
            foreach (var article in snapshot.ScrapeArticlesResult.ScrapedArticles)
                if (article.ScrapeExceptions is not null)
                    foreach (var exception in article.ScrapeExceptions)
                        scrapeErrors += $"{exception.Source}: {exception.Message} | ";

        string commandText = @"
            UPDATE news_snapshot
            SET finished_on = @finished_on,
                run_time_in_seconds = @run_time_in_seconds,
                sections_scraped = @sections_scraped, 
                headlines_scraped = @headlines_scraped,
                articles_scraped = @articles_scraped,
                is_success = @is_success, 
                job_error = @job_error,
                scrape_errors = @scrape_errors
            WHERE id = @id;";

        SqliteParameter[] parameters = [
            new("@id", snapshot.Id),
            new("@finished_on", (object?)snapshot.FinishedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@run_time_in_seconds", (object?)snapshot.RunTimeInSeconds ?? DBNull.Value),
            new("@sections_scraped", (object?)snapshot.ScrapeHeadlinesResult?.SectionsScraped ?? DBNull.Value),
            new("@headlines_scraped", (object?)snapshot.ScrapeHeadlinesResult?.HeadlinesScraped ?? DBNull.Value),
            new("@articles_scraped", (object?)snapshot.ScrapeArticlesResult?.ArticlesScraped ?? DBNull.Value),
            new("@is_success", snapshot.IsSuccess.HasValue ? (snapshot.IsSuccess.Value ? 1 : 0) : (object?)DBNull.Value),
            new("@job_error", !string.IsNullOrWhiteSpace(jobError) ? jobError : (object?)DBNull.Value),
            new("@scrape_errors", !string.IsNullOrWhiteSpace(scrapeErrors) ? scrapeErrors : (object?)DBNull.Value)
        ];

        int rowsAffected = await database.ExecuteNonQueryAsync(commandText, parameters);
        if (rowsAffected == 0)
            throw new InvalidOperationException($"No record found with id = {snapshot.Id} to update in the news_snapshot table.");
    }

    // Read operations
    //public async Task<ScrapeJob?> GetByIdAsync(long id)
    //public async Task<IEnumerable<ScrapeJob>> GetAllAsync()
    //public async Task<IEnumerable<ScrapeJob>> GetRecentAsync(int count)

    // Query operations
    //public async Task<bool> ExistsAsync(long id)
    //public async Task<int> CountAsync()
}
