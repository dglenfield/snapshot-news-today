using Microsoft.Data.Sqlite;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Data.Repositories;

public class ScrapedHeadlineRepository(SnapshotJobDatabase database)
{
    public async Task CreateTableAsync()
    {
        string commandText = @"
            CREATE TABLE IF NOT EXISTS scraped_headline (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                news_snapshot_id INTEGER, -- Foreign key to link to news_snapshot table
                section_name TEXT,
                headline TEXT,
                target_uri TEXT NOT NULL UNIQUE, -- target_uri is unique to prevent duplicate articles
                last_updated_on TEXT,
                most_read INTEGER,
                FOREIGN KEY(news_snapshot_id) REFERENCES news_snapshot(id) ON DELETE CASCADE);

            INSERT INTO database_info (entity, version) 
                VALUES ('scraped_headline', '1.1');";

        await database.ExecuteNonQueryAsync(commandText);
    }

    public async Task<long> CreateAsync(ScrapedHeadline headline, long jobId)
    {
        string commandText = @"
            INSERT INTO scraped_headline (
                news_snapshot_id, section_name, headline, target_uri, last_updated_on, most_read) 
            VALUES (@news_snapshot_id, @section_name, @headline, @target_uri, @last_updated_on, @most_read);";

        SqliteParameter[] parameters = [
            new("@news_snapshot_id", jobId),
            new("@section_name", (object?)headline.SectionName ?? DBNull.Value),
            new("@headline", headline.Headline),
            new("@target_uri", headline.TargetUri.AbsoluteUri),
            new("@last_updated_on", (object?)headline.LastUpdatedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@most_read", headline.MostRead ? 1 : 0)
        ];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException("Insert into scraped_headline failed, no row id returned.");
    }

    public async Task<bool> ExistsAsync(Uri targetUri)
    {
        string commandText = @"SELECT id FROM scraped_headline WHERE id > 0 AND target_uri = @target_uri LIMIT 1;";
        SqliteParameter[] parameters = [new("@target_uri", targetUri.AbsoluteUri)];

        var result = await database.ExecuteScalarAsync(commandText, parameters);
        return !string.IsNullOrWhiteSpace(result?.ToString());
    }
}
