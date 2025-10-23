using Common.Logging;
using Microsoft.Data.Sqlite;
using NewsScraper.Data.Providers;
using NewsScraper.Models;
using System.Data.Common;

namespace NewsScraper.Data;

internal class ScrapeJobRepository(ScrapeJobDataProvider dataProvider, Logger logger)
{
    public string DatabaseFilePath => dataProvider.DatabaseFilePath;

    public async Task<long> CreateScrapeJobAsync(ScrapeJob job)
    {
        string commandText = @"
            INSERT INTO scrape_job (source_name, source_uri, job_started_on) 
            VALUES (@source_name, @source_uri, @scrape_started_on);";
        SqliteParameter[] parameters = [
            new("@source_name", (object)job.SourceName),
            new("@source_uri", (object)job.SourceUri.AbsoluteUri),
            new("@scrape_started_on", (object?)job.JobStartedOn.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value)];
        try
        {
            long id = await dataProvider.InsertAsync(commandText, parameters);
            return id > 0 ? id : throw new InvalidOperationException("Insert scrape_job failed, no row id returned.");
        }
        catch (DbException)
        {
            logger.Log("Error inserting record into scrape_job.", LogLevel.Error);
            throw;
        }
    }

    public async Task UpdateScrapeJobAsync(ScrapeJob job)
    {
        string errorMessages = job.ScrapeException is not null ? $"{job.ScrapeException.Source}: {job.ScrapeException.Message} | " : string.Empty;
        if (job.PageScrapeResult?.ScrapeExceptions is not null)
            foreach (var exception in job.PageScrapeResult.ScrapeExceptions) 
                errorMessages += $"{exception.Source}: {exception.Message} | ";

        string commandText = @"
            UPDATE scrape_job
            SET sections_scraped = @sections_scraped, 
                headlines_scraped = @headlines_scraped,
                job_finished_on = @job_finished_on, 
                scrape_success = @scrape_success, 
                error_messages = @error_messages
            WHERE id = @id;";
        SqliteParameter[] parameters = [
            new("@id", job.Id),
            new("@sections_scraped", (object?)job.PageScrapeResult?.SectionsScraped ?? DBNull.Value),
            new("@headlines_scraped", (object?)job.PageScrapeResult?.HeadlinesScraped ?? DBNull.Value),
            new("@job_finished_on", (object?)job.JobFinishedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@scrape_success", job.Success.HasValue ? (job.Success.Value ? 1 : 0) : (object?)DBNull.Value),
            new("@error_messages", !string.IsNullOrWhiteSpace(errorMessages) ? errorMessages : (object?)DBNull.Value)];
        try
        {
            int rowsAffected = await dataProvider.ExecuteNonQueryAsync(commandText, parameters);
            if (rowsAffected == 0)
                throw new InvalidOperationException($"No record found with id = {job.Id} to update in the scrape_job table.");
        }
        catch (DbException)
        {
            logger.Log("Error updating record in scrape_job.", LogLevel.Error);
            throw;
        }
    }
}
