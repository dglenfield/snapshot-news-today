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
internal class ScrapeJobRunRepository(ScraperJobDataProvider dataProvider, Logger logger)
{
    public string DatabaseFilePath => _dataProvider.DatabaseFilePath;

    private readonly ScraperJobDataProvider _dataProvider = dataProvider;

    public async Task<long> CreateJobRunAsync()
    {
        string commandText = "INSERT INTO scrape_job_run (source_name, source_uri) VALUES (@source_name, @source_uri);";
        SqliteParameter[] parameters = [
            new("@source_name", (object)ScrapeJobRun.SourceName),
            new("@source_uri", (object)ScrapeJobRun.SourceUri.AbsoluteUri)];
        try
        {
            long id = await _dataProvider.InsertAsync(commandText, parameters);
            return id > 0 ? id : throw new InvalidOperationException("Insert scrape_job_run failed, no row id returned.");
        }
        catch (DbException)
        {
            logger.Log("Error inserting record into scrape_job_run.", LogLevel.Error);
            throw;
        }
    }

    public async Task<long> CreateNewsStoryArticleAsync(SourceNewsStory newsStory)
    {
        string commandText =
            @"INSERT INTO news_story_article (job_run_id, source_name, article_uri, category, story_headline, original_publish_date)
            VALUES (@job_run_id, @source_name, @article_uri, @category, @story_headline, @original_publish_date);";
        SqliteParameter[] parameters = [
            new("@job_run_id", (object?)newsStory.JobRunId ?? DBNull.Value),
            new("@source_name", (object?)newsStory.SourceName ?? DBNull.Value),
            new("@article_uri", (object?)newsStory.Article?.ArticleUri.AbsoluteUri ?? DBNull.Value),
            new("@category", (object?)newsStory.Category ?? DBNull.Value),
            new("@story_headline", (object?)newsStory.StoryHeadline ?? DBNull.Value),
            new("@original_publish_date", (object?)newsStory.Article?.PublishDate ?? DBNull.Value)];
        try
        {
            long id = await _dataProvider.InsertAsync(commandText, parameters);
            return id > 0 ? id : throw new InvalidOperationException("Insert news_story_article failed, no row id returned.");
        }
        catch (DbException)
        {
            logger.Log("Error inserting record into news_story_article.", LogLevel.Error);
            throw;
        }
    }

    public async Task UpdateJobRunAsync()
    {
        string commandText =
            @"UPDATE scrape_job_run
            SET news_stories_found = @news_stories_found, scrape_end = @scrape_end, success = @success, error_message = @error_message
            WHERE id = @id;";
        SqliteParameter[] parameters = [
            new("@id", ScrapeJobRun.Id),
            new("@news_stories_found", (object?)ScrapeJobRun.NewsStoriesFound ?? DBNull.Value),
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
            logger.Log("Error updating record in scrape_job_run.", LogLevel.Error);
            throw;
        }
    }
}
