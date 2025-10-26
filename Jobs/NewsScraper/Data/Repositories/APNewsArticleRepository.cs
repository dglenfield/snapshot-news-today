using Microsoft.Data.Sqlite;
using NewsScraper.Models.AssociatedPress.ArticlePage;
using NewsScraper.Models.AssociatedPress.MainPage;

namespace NewsScraper.Data.Repositories;

internal class APNewsArticleRepository(NewsScraperDatabase database)
{
    public async Task<long> CreateAsync(Article article)
    {
        string source = article.TestFile is null ? article.SourceUri.AbsoluteUri : article.TestFile;

        string commandText = @"
            INSERT INTO ap_news_article (
                headline_id, source)
            VALUES (
                @headline_id, @source);";

        SqliteParameter[] parameters = [
            new("@headline_id", article.HeadlineId),
            new("@source", source)];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException("Insert into ap_news_article failed, no row id returned.");
    }

    public async Task UpdateAsync(Article article)
    {
        if (article.Id == 0)
            throw new ArgumentException("Article must have a valid Id to update.", nameof(article));

        string commandText = @"
            UPDATE ap_news_article
            SET headline = @headline,
            author = @author,
            last_updated_on = @last_updated_on,
            article_content = @article_content, 
            is_success = @is_success,
            error_message = @error_message
            WHERE id = @id;";

        string? errorMessage = article.ScrapeException is not null ? $"{article.ScrapeException?.Source}: {article.ScrapeException?.Message}" : null;

        SqliteParameter[] parameters = [
            new("@id", article.Id),
            new("@headline", (object?)article.Headline ?? DBNull.Value),
            new("@author", (object?)article.Author ?? DBNull.Value),
            new("@last_updated_on", (object?)article.LastUpdatedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@article_content", (object?)(article.Content?.Length > 0 ? article.Content : DBNull.Value)),
            new("@is_success", article.IsSuccess is bool s ? (s ? 1 : 0) : (object?)DBNull.Value),
            new("@error_message", (object?)errorMessage ?? DBNull.Value)
        ];

        int rowsAffected = await database.ExecuteNonQueryAsync(commandText, parameters);
        if (rowsAffected == 0)
            throw new InvalidOperationException($"No record found with id {article.Id} to update in table ap_news_article.");
    }
}
