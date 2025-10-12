using Common.Logging;
using Microsoft.Data.Sqlite;
using NewsScraper.Data.Providers;
using NewsScraper.Models;
using System.Data.Common;

namespace NewsScraper.Data;

internal class NewsStoryArticleRepository(ScraperJobDataProvider dataProvider, Logger logger)
{
    public string DatabaseFilePath => dataProvider.DatabaseFilePath;

    public async Task<long> CreateNewsStoryArticleAsync(SourceNewsStory newsStory)
    {
        string commandText =
            @"INSERT INTO news_story_article 
            (job_run_id, source_name, article_uri, category, story_headline, original_publish_date)
            VALUES 
            (@job_run_id, @source_name, @article_uri, @category, @story_headline, @original_publish_date);";
        SqliteParameter[] parameters = [
            new("@job_run_id", (object?)newsStory.JobRunId ?? DBNull.Value),
            new("@source_name", (object?)newsStory.SourceName ?? DBNull.Value),
            new("@article_uri", (object?)newsStory.Article?.ArticleUri.AbsoluteUri ?? DBNull.Value),
            new("@category", (object?)newsStory.Category ?? DBNull.Value),
            new("@story_headline", (object?)newsStory.StoryHeadline ?? DBNull.Value),
            new("@original_publish_date", (object?)newsStory.Article?.PublishDate ?? DBNull.Value)];
        try
        {
            long id = await dataProvider.InsertAsync(commandText, parameters);
            return id > 0 ? id : throw new InvalidOperationException("Insert news_story_article failed, no row id returned.");
        }
        catch (DbException)
        {
            logger.Log("Error inserting record into news_story_article.", LogLevel.Error);
            throw;
        }
    }

    public async Task<bool> UpdateNewsStoryArticleAsync(SourceNewsStory newsStory)
    {
        if (newsStory.Id == 0)
            throw new ArgumentException("News story must have a valid Id to update.", nameof(newsStory));
        string commandText =
            @"UPDATE news_story_article
            SET article_headline = @article_headline, 
                author = @author,
                original_publish_date = @original_publish_date, 
                last_updated_date = @last_updated_date,
                is_paywalled = @is_paywalled, 
                article_content = @article_content, 
                success = @success, 
                error_message = @error_message
            WHERE id = @id;";
        SqliteParameter[] parameters = [
            new("@id", newsStory.Id),
            new("@article_headline", (object?)newsStory.Article?.Headline ?? DBNull.Value),
            new("@author", (object?)newsStory.Article?.Author ?? DBNull.Value),
            new("@original_publish_date", (object?)newsStory.Article?.PublishDate ?? DBNull.Value),
            new("@last_updated_date", (object?)newsStory.Article?.LastUpdatedDate ?? DBNull.Value),
            new("@is_paywalled", newsStory.Article?.IsPaywalled is bool p ? (p ? 1 : 0) : (object?)DBNull.Value),
            new("@article_content", (object?)newsStory.Article?.Content ?? DBNull.Value),
            new("@success", newsStory.Article?.Success is bool s ? (s ? 1 : 0) : (object?)DBNull.Value),
            new("@error_message", (object?)newsStory.Article?.ErrorMessage ?? DBNull.Value)
        ];
        try
        {
            int rowsAffected = await dataProvider.ExecuteNonQueryAsync(commandText, parameters);
            if (rowsAffected == 0)
                return false;
            return true;
            throw new InvalidOperationException($"No record found with id {newsStory.Id} to update in table news_story_article.");
        }
        catch (DbException)
        {
            logger.Log("Error updating record in news_story_article.", LogLevel.Error);
            throw;
        }
    }
}
