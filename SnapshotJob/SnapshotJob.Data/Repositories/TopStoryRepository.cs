using Microsoft.Data.Sqlite;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Data.Repositories;

public class TopStoryRepository(SnapshotJobDatabase database)
{
    public string Version { get; } = "1.1";

    public async Task<long> CreateAsync(TopStory topStory)
    {
        string commandText = @"
            INSERT INTO top_story (
                scraped_article_id, headline)
            VALUES (
                @scraped_article_id, @headline);";

        SqliteParameter[] parameters = [
            new("@scraped_article_id", topStory.ScrapedArticleId),
            new("@headline", topStory.Headline)];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException("Insert into top_story failed, no row id returned.");
    }

    public async Task<bool> ExistsAsync(long scrapeArticleId)
    {
        string commandText = @"SELECT scraped_article_id FROM top_story WHERE scraped_article_id = @scraped_article_id LIMIT 1;";
        SqliteParameter[] parameters = [new("@scraped_article_id", scrapeArticleId)];

        var result = await database.ExecuteScalarAsync(commandText, parameters);
        return !string.IsNullOrWhiteSpace(result?.ToString());
    }

    public async Task CreateTableAsync()
    {
        string commandText = $@"
            CREATE TABLE IF NOT EXISTS top_story (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                created_on TEXT NOT NULL DEFAULT (datetime('now', 'utc')),
                scraped_article_id INTEGER,
                headline TEXT,
                FOREIGN KEY(scraped_article_id) REFERENCES scraped_article(id) ON DELETE CASCADE);

            INSERT INTO database_info (entity, version) 
                VALUES ('top_story', '{Version}');";

        await database.ExecuteNonQueryAsync(commandText);
    }
}
