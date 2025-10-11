using Common.Logging;
using Microsoft.Data.Sqlite;
using NewsScraper.Data.Providers;
using NewsScraper.Models;
using System.Data.Common;

namespace NewsScraper.Data;

internal class ScrapeJobRunRepository(ScraperDataProvider dataProvider, Logger logger)
{
    public string DatabaseFilePath => _dataProvider.DatabaseFilePath;

    private readonly Logger _logger = logger;
    private readonly ScraperDataProvider _dataProvider = dataProvider;

    public async Task<long> CreateJobRunAsync()
    {
        string commandText = "INSERT INTO scrape_job_run (source_name, source_uri) VALUES (@source_name, @source_uri);";
        SqliteParameter[] parameters = [
            new("@source_name", (object)ScrapeJobRun.SourceName),
            new("@source_uri", (object)ScrapeJobRun.SourceUri.AbsoluteUri)];
        try
        {
            long id = await _dataProvider.InsertAsync(commandText, parameters);
            if (id == -1)
                throw new InvalidOperationException("Insert scrape_job_run failed, no row id returned.");
            return id;
        }
        catch (DbException)
        {
            _logger.Log("Error inserting record into scrape_job_run.", LogLevel.Error);
            throw;
        }
    }

    public async Task<long> CreateSourceArticleAsync(SourceArticle sourceArticle)
    {
        string commandText =
            @"INSERT INTO source_article (job_run_id, source_name, article_uri, headline, publish_date, category)
            VALUES (@job_run_id, @source_name, @article_uri, @headline, @publish_date, @category);";
        SqliteParameter[] parameters = [
            new("@job_run_id", (object?)sourceArticle.JobRunId ?? DBNull.Value),
            new("@source_name", (object?)sourceArticle.SourceName ?? DBNull.Value),
            new("@article_uri", (object?)sourceArticle.SourceUri.AbsoluteUri ?? DBNull.Value),
            new("@headline", (object?)sourceArticle.SourceHeadline ?? DBNull.Value),
            new("@publish_date", (object?)sourceArticle.SourcePublishDate ?? DBNull.Value),
            new("@category", (object?)sourceArticle.SourceCategory ?? DBNull.Value)];
        try
        {
            long id = await _dataProvider.InsertAsync(commandText, parameters);
            if (id == -1)
                throw new InvalidOperationException("Insert source_article failed, no row id returned.");
            return id;
        }
        catch (DbException)
        {
            _logger.Log("Error inserting record into source_article.", LogLevel.Error);
            throw;
        }
    }

    public async Task UpdateJobRunAsync()
    {
        string commandText =
            @"UPDATE scrape_job_run
            SET source_articles_found = @source_articles_found, scrape_end = @scrape_end, success = @success, error_message = @error_message
            WHERE id = @id;";
        SqliteParameter[] parameters = [
            new("@id", ScrapeJobRun.Id),
            new("@source_articles_found", (object?)ScrapeJobRun.SourceArticlesFound ?? DBNull.Value),
            new("@scrape_end", (object?)ScrapeJobRun.ScrapeEnd?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@success", ScrapeJobRun.Success.HasValue ? (ScrapeJobRun.Success.Value ? 1 : 0) : (object?)DBNull.Value),
            new("@error_message", (object?)ScrapeJobRun.ErrorMessage ?? DBNull.Value)];
        try
        {
            int rowsAffected = await _dataProvider.ExecuteNonQueryAsync(commandText, parameters);
            if (rowsAffected == 0)
                throw new InvalidOperationException($"No record found with id {ScrapeJobRun.Id} to update in table scrape_job_run.");
        }
        catch (DbException)
        {
            _logger.Log("Error updating record in scrape_job_run.", LogLevel.Error);
            throw;
        }
    }
}
