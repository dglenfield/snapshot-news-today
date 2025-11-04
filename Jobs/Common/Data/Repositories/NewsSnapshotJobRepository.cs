using Common.Models;
using Microsoft.Data.Sqlite;

namespace Common.Data.Repositories;

public class NewsSnapshotJobRepository(NewsSnapshotDatabase database)
{
    public async Task<long> CreateAsync(NewsSnapshotJob job)
    {
        string commandText = @"
            INSERT INTO news_snapshot_job (started_on) 
            VALUES (@started_on);";

        SqliteParameter[] parameters = [
            new("@started_on", (object?)job.StartedOn.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value)
        ];

        long id = await database.InsertAsync(commandText, parameters);
        if (id > 0)
            return id;

        throw new InvalidOperationException("Insert into news_snapshot_job failed, no row id returned.");
    }

    public async Task CreateTableAsync()
    {
        string script = "CreateNewsSnapshotJobTableV1.1";
        string scriptFilePath = Path.Combine(AppContext.BaseDirectory, "Data\\Scripts", script);
        string scriptContent = File.ReadAllText(scriptFilePath);

        await database.ExecuteNonQueryAsync(scriptContent);
    }

    public async Task UpdateAsync(NewsSnapshotJob job)
    {
        string jobError = job.JobException is not null ? $"{job.JobException.Source}: {job.JobException.Message}" : string.Empty;

        string commandText = @"
            UPDATE news_snapshot_job
            SET finished_on = @finished_on,
                run_time_in_seconds = @run_time_in_seconds,
                is_success = @is_success, 
                error_message = @error_message
            WHERE id = @id;";

        SqliteParameter[] parameters = [
            new("@id", job.Id),
            new("@finished_on", (object?)job.FinishedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@run_time_in_seconds", (object?)job.RunTimeInSeconds ?? DBNull.Value),
            new("@is_success", job.IsSuccess.HasValue ? (job.IsSuccess.Value ? 1 : 0) : (object?)DBNull.Value),
            new("@error_message", !string.IsNullOrWhiteSpace(jobError) ? jobError : (object?)DBNull.Value)
        ];

        int rowsAffected = await database.ExecuteNonQueryAsync(commandText, parameters);
        if (rowsAffected == 0)
            throw new InvalidOperationException($"No record found with id = {job.Id} to update in the news_snapshot_job table.");
    }
}
