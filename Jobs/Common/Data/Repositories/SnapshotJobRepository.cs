using Common.Models;
using Microsoft.Data.Sqlite;

namespace Common.Data.Repositories;

public class SnapshotJobRepository(NewsSnapshotDatabase database)
{
    public async Task<long> CreateAsync(SnapshotJob job)
    {
        string commandText = @"
            INSERT INTO snapshot_job (started_on) 
            VALUES (@started_on);";

        SqliteParameter[] parameters = [
            new("@started_on", (object?)job.StartedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value)
        ];

        long id = await database.InsertAsync(commandText, parameters);
        if (id > 0)
            return id;

        throw new InvalidOperationException("Insert into snapshot_job failed, no row id returned.");
    }

    public async Task CreateTableAsync()
    {
        string script = "CreateSnapshotJobTableV1.1";
        string scriptFilePath = Path.Combine(AppContext.BaseDirectory, "Data\\Scripts", script);
        string scriptContent = File.ReadAllText(scriptFilePath);

        await database.ExecuteNonQueryAsync(scriptContent);
    }

    public async Task UpdateAsync(SnapshotJob job)
    {
        string jobError = job.JobException is not null ? $"{job.JobException.Source}: {job.JobException.Message}" : string.Empty;

        string scrapeErrors = string.Empty;
        if (job.ScrapeHeadlinesResult?.ScrapeExceptions is not null)
            foreach (var exception in job.ScrapeHeadlinesResult.ScrapeExceptions)
                scrapeErrors += $"{exception.Source}: {exception.Message} | ";
        if (job.ScrapeArticlesResult?.ScrapeExceptions is not null)
            foreach (var exception in job.ScrapeArticlesResult.ScrapeExceptions)
                scrapeErrors += $"{exception.Source}: {exception.Message} | ";

        string commandText = @"
            UPDATE snapshot_job
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
            new("@id", job.Id),
            new("@finished_on", (object?)job.FinishedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@run_time_in_seconds", (object?)job.RunTimeInSeconds ?? DBNull.Value),
            new("@sections_scraped", (object?)job.ScrapeHeadlinesResult?.SectionsScraped ?? DBNull.Value),
            new("@headlines_scraped", (object?)job.ScrapeHeadlinesResult?.HeadlinesScraped ?? DBNull.Value),
            new("@articles_scraped", (object?)job.ScrapeArticlesResult?.ArticlesScraped ?? DBNull.Value),
            new("@is_success", job.IsSuccess.HasValue ? (job.IsSuccess.Value ? 1 : 0) : (object?)DBNull.Value),
            new("@job_error", !string.IsNullOrWhiteSpace(jobError) ? jobError : (object?)DBNull.Value),
            new("@scrape_errors", !string.IsNullOrWhiteSpace(scrapeErrors) ? scrapeErrors : (object?)DBNull.Value)
        ];

        int rowsAffected = await database.ExecuteNonQueryAsync(commandText, parameters);
        if (rowsAffected == 0)
            throw new InvalidOperationException($"No record found with id = {job.Id} to update in the snapshot_job table.");
    }

    // Read operations
    //public async Task<ScrapeJob?> GetByIdAsync(long id)
    //public async Task<IEnumerable<ScrapeJob>> GetAllAsync()
    //public async Task<IEnumerable<ScrapeJob>> GetRecentAsync(int count)

    // Query operations
    //public async Task<bool> ExistsAsync(long id)
    //public async Task<int> CountAsync()
}
