using Common.Configuration.Options;
using Common.Data;
using Microsoft.Extensions.Options;

namespace NewsAnalyzer.Data;

public class NewsAnalyzerDatabase(IOptions<DatabaseOptions> options) : SqliteDatabase(options.Value.DatabaseFilePath)
{
    public async Task<bool> CreateAsync()
    {
        bool overwriteFlag = options.Value.DatabaseVersion.EndsWith("-overwrite", StringComparison.OrdinalIgnoreCase);
        if (File.Exists(DatabaseFilePath) && !overwriteFlag)
            return false; // Database file already exists, no need to create it again

        // We don't want to delete the database
        // Drop the table instead
        //if (File.Exists(DatabaseFilePath) && overwriteFlag)
        //    await DeleteAsync(); // Delete existing database if overwrite flag is set

        await CreateSnapshotNewsJobTableAsync();
        await CreateNewsAnalyzerJobTableAsync();

        return true;
    }

    private async Task CreateSnapshotNewsJobTableAsync()
    {
        string commandText = @"
                CREATE TABLE IF NOT EXISTS snapshot_news_job (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    job_started_on TEXT NOT NULL,
                    job_finished_on TEXT,
                    run_time_in_seconds INTEGER,
                    is_success INTEGER,
                    job_error TEXT);";
        await ExecuteNonQueryAsync(commandText);
    }

    private async Task CreateNewsAnalyzerJobTableAsync()
    {
        string commandText = @"
                CREATE TABLE IF NOT EXISTS news_analyzer_job (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    snapshot_news_job_id INTEGER NOT NULL,
                    job_started_on TEXT NOT NULL,
                    job_finished_on TEXT,
                    run_time_in_seconds INTEGER,
                    is_success INTEGER,
                    job_error TEXT,
                    FOREIGN KEY(snapshot_news_job_id) REFERENCES snapshot_news_job(id) ON DELETE CASCADE);";
        await ExecuteNonQueryAsync(commandText);
    }
}
