namespace Common.Data.Repositories;

public class NewsAnalysisRepository(SnapshotJobDatabase database)
{
    public async Task CreateTableAsync()
    {
        string commandText = @"
            CREATE TABLE IF NOT EXISTS news_analysis (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                news_snapshot_id INTEGER NOT NULL,
                run_time_in_seconds INTEGER,
                is_success INTEGER,
                error_message TEXT,
                FOREIGN KEY(news_snapshot_id) REFERENCES news_snapshot(id) ON DELETE CASCADE);

            INSERT INTO database_info (entity, version) 
                VALUES ('news_analysis', '1.1');";

        await database.ExecuteNonQueryAsync(commandText);
    }
}
