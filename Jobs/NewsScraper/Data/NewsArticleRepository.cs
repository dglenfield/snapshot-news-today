using Common.Logging;
using Microsoft.Data.Sqlite;
using NewsScraper.Data.Providers;
using NewsScraper.Models.CNN;
using System.Data.Common;

namespace NewsScraper.Data;

internal class NewsArticleRepository(ScrapeJobDataProvider dataProvider, Logger logger)
{
    public string DatabaseFilePath => dataProvider.DatabaseFilePath;

    public async Task<long> CreateNewsArticleAsync(Article article)
    {
        string commandText = @"
            INSERT INTO news_article (
                job_run_id, source_name, article_uri, category, story_headline, original_publish_date)
            VALUES (
                @job_run_id, @source_name, @article_uri, @category, @story_headline, @original_publish_date);";
        SqliteParameter[] parameters = [
            new("@job_run_id", (object?)article.JobRunId ?? DBNull.Value),
            new("@source_name", (object?)article.SourceName ?? DBNull.Value),
            new("@article_uri", (object?)article.ArticleUri.AbsoluteUri ?? DBNull.Value),
            new("@category", (object?)article.Category ?? DBNull.Value),
            new("@story_headline", (object?)article.Headline ?? DBNull.Value),
            new("@original_publish_date", (object?)article.PublishDate ?? DBNull.Value)];
        try
        {
            long id = await dataProvider.InsertAsync(commandText, parameters);
            return id > 0 ? id : throw new InvalidOperationException("Insert news_article failed, no row id returned.");
        }
        catch (DbException)
        {
            logger.Log("Error inserting record into news_article.", LogLevel.Error);
            throw;
        }
    }

    public async Task<bool> UpdateNewsArticleAsync(Article article)
    {
        if (article.Id == 0)
            throw new ArgumentException("News article must have a valid Id to update.", nameof(article));
        string commandText = @"
            UPDATE news_article
            SET headline = @headline, 
                author = @author,
                original_publish_date = @original_publish_date, 
                last_updated_date = @last_updated_date,
                article_content = @article_content, 
                success = @success, 
                error_message = @error_message
            WHERE id = @id;";
        SqliteParameter[] parameters = [
            new("@id", article.Id),
            new("@headline", (object?)article.Headline ?? DBNull.Value),
            new("@author", (object?)article.Author ?? DBNull.Value),
            new("@original_publish_date", (object?)article.PublishDate ?? DBNull.Value),
            new("@last_updated_date", (object?)article.LastUpdatedDate ?? DBNull.Value),
            new("@article_content", (object?)article.Content ?? DBNull.Value),
            new("@success", article.Success is bool s ? (s ? 1 : 0) : (object?)DBNull.Value),
            new("@error_message", (object?)article.ErrorMessage ?? DBNull.Value)
        ];
        try
        {
            int rowsAffected = await dataProvider.ExecuteNonQueryAsync(commandText, parameters);
            if (rowsAffected == 0)
                return false;
            return true;
            throw new InvalidOperationException($"No record found with id {article.Id} to update in table news_article.");
        }
        catch (DbException)
        {
            logger.Log("Error updating record in news_article.", LogLevel.Error);
            throw;
        }
    }
}
