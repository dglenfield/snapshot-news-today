namespace SnapshotJob.Data.Repositories;

public class TopStoryRepository(SnapshotJobDatabase database)
{
    public async Task CreateTableAsync()
    {
        string commandText = @"
            CREATE TABLE IF NOT EXISTS top_story (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                scraped_article_id INTEGER,
                section_name TEXT,
                headline TEXT,
                article_uri TEXT NOT NULL UNIQUE, -- article_uri is unique to prevent duplicate articles
                last_updated_on TEXT,
                author TEXT,
                most_read INTEGER,
                article_content TEXT,
                is_success INTEGER,
                FOREIGN KEY(scraped_article_id) REFERENCES scraped_article(id) ON DELETE CASCADE);

            INSERT INTO database_info (entity, version) 
                VALUES ('top_story', '1.1');";

        await database.ExecuteNonQueryAsync(commandText);
    }
}
