using Microsoft.Data.Sqlite;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Data.Repositories;

public class NewsSnapshotArticleRepository(SnapshotJobDatabase database)
{
    public float Version { get; } = 1.1F;

    public async Task<long> CreateAsync(NewsSnapshotArticle record)
    {
        string commandText = @"
            INSERT INTO news_snapshot_article (
                news_snapshot_id, analyzed_article_id, 
                source_uri, source_section_name, last_updated_on, author, source_headline,
                custom_headline, summary, key_points, article_content)
            VALUES (
                @news_snapshot_id, @analyzed_article_id, 
                @source_uri, @source_section_name, @last_updated_on, @author, @source_headline,
                @custom_headline, @summary, @key_points, @article_content);";

        SqliteParameter[] parameters = [
            new("@news_snapshot_id", record.NewsSnapshotId),
            new("@analyzed_article_id", record.AnalyzedArticleId),
            new("@source_uri", record.SourceUri.AbsoluteUri),
            new("@source_section_name", string.IsNullOrWhiteSpace(record.SourceSectionName) 
                ? DBNull.Value : record.SourceSectionName),
            new("@last_updated_on", (object?)record.LastUpdatedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@author", string.IsNullOrWhiteSpace(record.Author) ? DBNull.Value : record.Author),
            new("@source_headline", record.SourceHeadline),
            new("@custom_headline", string.IsNullOrWhiteSpace(record.CustomHeadline) ? DBNull.Value : record.CustomHeadline),
            new("@summary", string.IsNullOrWhiteSpace(record.Summary) ? DBNull.Value : record.Summary),
            new("@key_points", string.IsNullOrWhiteSpace(record.KeyPointsJson) ? DBNull.Value : record.KeyPointsJson),
            new("@article_content", (object?)(record.Content?.Length > 0 ? record.Content : DBNull.Value))
            ];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException("Insert into news_snapshot_article failed, no row id returned.");
    }

    public async Task<bool> ExistsAsync(Uri targetUri)
    {
        string commandText = @"SELECT id FROM news_snapshot_article WHERE id > 0 AND source_uri = @source_uri LIMIT 1;";
        SqliteParameter[] parameters = [new("@source_uri", targetUri.AbsoluteUri)];

        var result = await database.ExecuteScalarAsync(commandText, parameters);
        return !string.IsNullOrWhiteSpace(result?.ToString());
    }

    public async Task<List<NewsSnapshotArticle>> GetBySnapshotId(long snapshotId)
    {
        string commandText = @"
            SELECT id, news_snapshot_id, analyzed_article_id, created_on, published_on,
                source_uri, source_section_name, author, source_headline, last_updated_on,
                custom_headline, summary, key_points, article_content 
            FROM news_snapshot_article 
            WHERE news_snapshot_id = @news_snapshot_id ORDER BY id;";

        SqliteParameter[] parameters = [new("@news_snapshot_id", snapshotId)];
        await using var reader = await database.ExecuteReaderAsync(commandText, parameters);

        var articles = new List<NewsSnapshotArticle>();
        while (await reader.ReadAsync())
        {
            articles.Add(new NewsSnapshotArticle
            {
                AnalyzedArticleId = reader.GetInt64(reader.GetOrdinal("analyzed_article_id")),
                Author = reader.IsDBNull(reader.GetOrdinal("author")) ? null : reader.GetString(reader.GetOrdinal("author")),
                ContentParagraphs = [reader.GetString(reader.GetOrdinal("article_content"))],
                CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on"))
                    ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("created_on"))),
                CustomHeadline = reader.IsDBNull(reader.GetOrdinal("custom_headline"))
                    ? null : reader.GetString(reader.GetOrdinal("custom_headline")),
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                KeyPoints = reader.IsDBNull(reader.GetOrdinal("custom_headline")) 
                    ? null : [reader.GetString(reader.GetOrdinal("key_points"))],
                KeyPointsJson = reader.IsDBNull(reader.GetOrdinal("custom_headline"))
                    ? null : reader.GetString(reader.GetOrdinal("key_points")),
                LastUpdatedOn = reader.IsDBNull(reader.GetOrdinal("last_updated_on"))
                    ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("last_updated_on"))),
                NewsSnapshotId = reader.GetInt64(reader.GetOrdinal("news_snapshot_id")),
                PublishedOn = reader.IsDBNull(reader.GetOrdinal("published_on"))
                    ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("published_on"))),
                SourceHeadline = reader.GetString(reader.GetOrdinal("source_headline")),
                SourceSectionName = reader.IsDBNull(reader.GetOrdinal("source_section_name"))
                    ? null : reader.GetString(reader.GetOrdinal("source_section_name")),
                SourceUri = new(reader.GetString(reader.GetOrdinal("source_uri"))),
                Summary = reader.IsDBNull(reader.GetOrdinal("summary"))
                    ? null : reader.GetString(reader.GetOrdinal("summary"))
            });
        }

        if (articles.Count == 0)
            throw new InvalidOperationException($"Snapshot Id {snapshotId} not found.");

        return articles;
    }

    public async Task CreateTableAsync()
    {
        string commandText = $@"
            CREATE TABLE IF NOT EXISTS news_snapshot_article (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                news_snapshot_id INTEGER, -- Foreign key to link to news_snapshot table
                analyzed_article_id INTEGER, -- Foreign key to link to analyzed_article table
                created_on TEXT NOT NULL DEFAULT (datetime('now', 'utc')),
                published_on TEXT,
                source_uri TEXT NOT NULL UNIQUE, -- source_uri is unique to prevent duplicate articles
                source_section_name TEXT,
                last_updated_on TEXT,
                author TEXT,
                source_headline TEXT NOT NULL,
                custom_headline TEXT,
                summary TEXT,
                key_points TEXT,
                article_content TEXT,
                FOREIGN KEY(news_snapshot_id) REFERENCES news_snapshot(id) ON DELETE CASCADE,
                FOREIGN KEY(analyzed_article_id) REFERENCES analyzed_article(id) ON DELETE CASCADE);

            INSERT INTO database_info (entity, version) 
                VALUES ('news_snapshot_article', '{Version}');";

        await database.ExecuteNonQueryAsync(commandText);
    }
}
