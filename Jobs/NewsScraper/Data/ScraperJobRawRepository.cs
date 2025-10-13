using Common.Logging;
using Microsoft.Data.Sqlite;
using NewsScraper.Data.Providers;
using NewsScraper.Models;
using System.Data.Common;

namespace NewsScraper.Data;

internal class ScraperJobRawRepository(ScraperJobRawDataProvider dataProvider, Logger logger)
{
    public string DatabaseFilePath => dataProvider.DatabaseFilePath;

    public async Task<long> CreateNewsArticleScrapeAsync(long newsStoryId, Uri articleUri, string rawContent)
    {
        string commandText = @"
            INSERT INTO news_article_scrape (
                scrape_job_run_id, news_story_article_id, article_uri, raw_content)
            VALUES (
                @scrape_job_run_id, @news_story_article_id, @article_uri, @raw_content);";
        SqliteParameter[] parameters = [
            new("@scrape_job_run_id", (object?)ScrapeJobRun.Id ?? DBNull.Value),
            new("@news_story_article_id", (object?)newsStoryId ?? DBNull.Value),
            new("@article_uri", (object?)articleUri.AbsoluteUri ?? DBNull.Value),
            new("@raw_content", (object?)rawContent ?? DBNull.Value)];
        try
        {
            long id = await dataProvider.InsertAsync(commandText, parameters);
            return id > 0 ? id : throw new InvalidOperationException("Insert news_article_scrape failed, no row id returned.");
        }
        catch (DbException)
        {
            logger.Log("Error inserting record into news_article_scrape.", LogLevel.Error);
            throw;
        }
    }
}
