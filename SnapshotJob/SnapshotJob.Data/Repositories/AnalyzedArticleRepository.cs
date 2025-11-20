using Microsoft.Data.Sqlite;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Data.Repositories;

public class AnalyzedArticleRepository(SnapshotJobDatabase database)
{
    public float Version { get; } = 1.1F;

    public async Task<long> CreateAsync(AnalyzedArticle record)
    {
        string error = record.Exception is not null
            ? $"{record.Exception.Source}: {record.Exception.Message}" : string.Empty;

        string commandText = @"
            INSERT INTO analyzed_article (
                scraped_article_id, custom_headline, summary,
                key_points, error)
            VALUES (
                @scraped_article_id, @custom_headline, @summary,
                @key_points, @error);";

        SqliteParameter[] parameters = [
            new("@scraped_article_id", record.ScrapedArticleId),
            new("@custom_headline", record.CustomHeadline),
            new("@summary", record.Summary),
            new("@key_points", record.KeyPointsJson),
            new("@error", !string.IsNullOrWhiteSpace(error) ? error : (object?)DBNull.Value)
            ];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException("Insert into analyzed_article failed, no row id returned.");
    }

    public async Task CreateTableAsync()
    {
        string commandText = $@"
            CREATE TABLE IF NOT EXISTS analyzed_article (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                scraped_article_id INTEGER, -- Foreign key to link to scraped_article table
                analyzed_on TEXT NOT NULL DEFAULT (datetime('now', 'utc')),
                custom_headline TEXT,
                summary TEXT,
                key_points TEXT,
                error TEXT,
                FOREIGN KEY(scraped_article_id) REFERENCES scraped_article(id) ON DELETE CASCADE);

            INSERT INTO database_info (entity, version) 
                VALUES ('analyzed_article', '{Version}');";

        await database.ExecuteNonQueryAsync(commandText);
    }
}
