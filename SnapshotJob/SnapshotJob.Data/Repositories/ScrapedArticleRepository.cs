using Microsoft.Data.Sqlite;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Data.Repositories;

public class ScrapedArticleRepository(SnapshotJobDatabase database)
{
    public float Version { get; } = 1.1F;

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

    public async Task<ScrapedArticle?> GetByIdAsync(long id)
    {
        string commandText = "SELECT * FROM scraped_article WHERE id = @id;";
        SqliteParameter[] parameters = [new("@id", id)];

        await using var reader = await database.ExecuteReaderAsync(commandText, parameters);
        if (await reader.ReadAsync())
        {
            return new ScrapedArticle()
            {
                Author = reader.IsDBNull(reader.GetOrdinal("author")) ? null : reader.GetString(reader.GetOrdinal("author")), 
                ContentParagraphs = [reader.GetString(reader.GetOrdinal("article_content"))],
                Headline = reader.GetString(reader.GetOrdinal("headline")),
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                IsSuccess = reader.GetBoolean(reader.GetOrdinal("is_success")),
                LastUpdatedOn = reader.IsDBNull(reader.GetOrdinal("last_updated_on"))
                    ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("last_updated_on"))),
                ScrapedHeadlineId = reader.GetInt64(reader.GetOrdinal("scraped_headline_id")),
                SourceUri = new(reader.GetString(reader.GetOrdinal("source")))
            };
        }

        return null;
    }

    public async Task<List<ScrapedArticle>> GetBySnapshotId(long snapshotId)
    {
        string commandText = @"
            SELECT article.id, article.last_updated_on, headline.section_name, article.headline, article.source, 
                   article.scraped_headline_id, article.is_success
            FROM news_snapshot snapshot
            inner join scraped_headline headline on headline.news_snapshot_id = snapshot.id
            inner join scraped_article article on article.scraped_headline_id = headline.id
            WHERE snapshot.Id = @snapshot_id ORDER BY article.last_updated_on DESC;";

        SqliteParameter[] parameters = [new("@snapshot_id", snapshotId)];
        await using var reader = await database.ExecuteReaderAsync(commandText, parameters);
        
        var articles = new List<ScrapedArticle>();
        while (await reader.ReadAsync())
        {
            articles.Add(new ScrapedArticle
            {
                Id = reader.GetInt64(0), // article.id
                LastUpdatedOn = reader.GetDateTime(1), // article.last_updated_on
                SectionName = reader.GetString(2), // headline.section_name
                Headline = reader.GetString(3), // article.headline
                SourceUri = new(reader.GetString(4)),
                ScrapedHeadlineId = reader.GetInt64(5),
                IsSuccess = reader.GetBoolean(6)
            });
        }

        if (articles.Count == 0)
            throw new InvalidOperationException("Snapshot Id not found.");

        return articles;
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

    public async Task CreateTableAsync()
    {
        string commandText = $@"
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
                VALUES ('scraped_article', '{Version}');";

        await database.ExecuteNonQueryAsync(commandText);
    }
}
