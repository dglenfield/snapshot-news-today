using Microsoft.Data.Sqlite;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Data.Repositories;

public class TopStoryRepository(SnapshotJobDatabase database)
{
    public async Task CreateTableAsync()
    {
        string commandText = @"
            CREATE TABLE IF NOT EXISTS top_story (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                scraped_article_id INTEGER,
                headline TEXT,
                FOREIGN KEY(scraped_article_id) REFERENCES scraped_article(id) ON DELETE CASCADE);

            INSERT INTO database_info (entity, version) 
                VALUES ('top_story', '1.1');";

        await database.ExecuteNonQueryAsync(commandText);
    }

    public async Task<long> CreateAsync(TopStory topStory)
    {
        string commandText = @"
            INSERT INTO scraped_article (
                scraped_article_id, headline)
            VALUES (
                @scraped_article_id, @headline);";

        SqliteParameter[] parameters = [
            new("@scraped_article_id", topStory.ScrapedArticleId),
            new("@headline", topStory.Headline)];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException("Insert into top_story failed, no row id returned.");
    }
}
