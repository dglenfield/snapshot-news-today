using Common.Logging;
using Microsoft.Data.Sqlite;
using NewsScraper.Data.Providers;
using NewsScraper.Models;
using System.Data.Common;

namespace NewsScraper.Data;

/// <summary>
/// Provides methods for creating and updating scrape job run and news story article records.
/// </summary>
/// <param name="dataProvider">The data provider used to execute database commands and manage connections for scrape job run and news story article
/// records.</param>
/// <param name="logger">The logger used to record errors and operational events during database operations.</param>
internal class ScraperJobRunRepository(ScraperJobDataProvider dataProvider, Logger logger)
{
    public string DatabaseFilePath => dataProvider.DatabaseFilePath;

    public async Task<long> CreateJobRunAsync()
    {
        string commandText = "INSERT INTO scrape_job_run (source_name, source_uri) VALUES (@source_name, @source_uri);";
        SqliteParameter[] parameters = [
            new("@source_name", (object)ScrapeJobRun.SourceName),
            new("@source_uri", (object)ScrapeJobRun.SourceUri.AbsoluteUri)];
        try
        {
            long id = await dataProvider.InsertAsync(commandText, parameters);
            return id > 0 ? id : throw new InvalidOperationException("Insert scrape_job_run failed, no row id returned.");
        }
        catch (DbException)
        {
            logger.Log("Error inserting record into scrape_job_run.", LogLevel.Error);
            throw;
        }
    }

    public async Task UpdateJobRunAsync()
    {
        string commandText = @"
            UPDATE scrape_job_run
            SET news_articles_found = @news_articles_found, 
                news_articles_scraped = @news_articles_scraped, 
                scrape_end = @scrape_end, 
                success = @success, 
                error_message = @error_message
            WHERE id = @id;";
        SqliteParameter[] parameters = [
            new("@id", ScrapeJobRun.Id),
            new("@news_articles_found", (object?)ScrapeJobRun.NewsArticlesFound ?? DBNull.Value),
            new("@news_articles_scraped", (object?)ScrapeJobRun.NewsArticlesScraped ?? DBNull.Value),
            new("@scrape_end", (object?)ScrapeJobRun.ScrapeEnd?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@success", ScrapeJobRun.Success.HasValue ? (ScrapeJobRun.Success.Value ? 1 : 0) : (object?)DBNull.Value),
            new("@error_message", (object?)ScrapeJobRun.ErrorMessage ?? DBNull.Value)];
        try
        {
            int rowsAffected = await dataProvider.ExecuteNonQueryAsync(commandText, parameters);
            if (rowsAffected == 0)
                throw new InvalidOperationException($"No record found with id {ScrapeJobRun.Id} to update in table scrape_job_run.");
        }
        catch (DbException)
        {
            logger.Log("Error updating record in scrape_job_run.", LogLevel.Error);
            throw;
        }
    }
}
