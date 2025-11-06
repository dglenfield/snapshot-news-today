using Microsoft.Data.Sqlite;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Data.Repositories;

public class ScrapedArticleRepository(SnapshotJobDatabase database)
{
    public async Task CreateTableAsync()
    {
        string commandText = @"
            CREATE TABLE IF NOT EXISTS scraped_article (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                scraped_headline_id INTEGER, -- Foreign key to link to scraped_headline table
                scraped_on TEXT NOT NULL DEFAULT (datetime('now', 'utc')),
                is_success INTEGER,
                source TEXT NOT NULL,
                headline TEXT,
                author TEXT,
                last_updated_on TEXT,
                article_content TEXT,
                errors TEXT,
                FOREIGN KEY(scraped_headline_id) REFERENCES scraped_headline(id) ON DELETE CASCADE);

            INSERT INTO database_info (entity, version) 
                VALUES ('scraped_article', '1.1');";

        await database.ExecuteNonQueryAsync(commandText);
    }

    public async Task<long> CreateAsync(ScrapedArticle article)
    {
        string source = article.TestFile is null ? article.SourceUri.AbsoluteUri : article.TestFile;

        string commandText = @"
            INSERT INTO scraped_article (
                scraped_headline_id, source)
            VALUES (
                @scraped_headline_id, @source);";

        SqliteParameter[] parameters = [
            new("@scraped_headline_id", article.ScrapedHeadlineId),
            new("@source", source)];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException("Insert into scraped_article failed, no row id returned.");
    }

    public async Task UpdateAsync(ScrapedArticle article)
    {
        if (article.Id == 0)
            throw new ArgumentException("Scraped article must have a valid Id.", nameof(article));

        string commandText = @"
            UPDATE scraped_article
            SET headline = @headline,
            author = @author,
            last_updated_on = @last_updated_on,
            article_content = @article_content, 
            is_success = @is_success,
            errors = @errors
            WHERE id = @id;";

        string errors = string.Empty;
        if (article.Exceptions is not null)
            foreach (var exception in article.Exceptions)
                errors += $"{exception.Source}: {exception.Message} | ";

        SqliteParameter[] parameters = [
            new("@id", article.Id),
            new("@headline", (object?)article.Headline ?? DBNull.Value),
            new("@author", (object?)article.Author ?? DBNull.Value),
            new("@last_updated_on", (object?)article.LastUpdatedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@article_content", (object?)(article.Content?.Length > 0 ? article.Content : DBNull.Value)),
            new("@is_success", article.IsSuccess is bool s ? (s ? 1 : 0) : (object?)DBNull.Value),
            new("@errors", !string.IsNullOrWhiteSpace(errors) ? errors : (object?)DBNull.Value)
        ];

        int rowsAffected = await database.ExecuteNonQueryAsync(commandText, parameters);
        if (rowsAffected == 0)
            throw new InvalidOperationException($"No record found with id {article.Id} to update in table scraped_article.");
    }
}
