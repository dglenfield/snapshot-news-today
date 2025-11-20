using Microsoft.Data.Sqlite;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Data.Repositories;

public class PerplexityApiCallRepository(SnapshotJobDatabase database)
{
    public float Version { get; } = 1.5F;

    public async Task<long> CreateAsync(PerplexityApiCall record, long snapshotId)
    {
        string error = record.Exception is not null
            ? $"{record.Exception.Source}: {record.Exception.Message}" : string.Empty;

        string commandText = @"
            INSERT INTO perplexity_api_call (
                news_snapshot_id, prompt_tokens, completion_tokens, total_tokens,
                input_tokens_cost, output_tokens_cost, request_cost, total_cost, 
                request_body, response_string, error)
            VALUES (
                @news_snapshot_id, @prompt_tokens, @completion_tokens, @total_tokens,
                @input_tokens_cost, @output_tokens_cost, @request_cost, @total_cost, 
                @request_body, @response_string, @error);";

        SqliteParameter[] parameters = [
            new("@news_snapshot_id", snapshotId),
            new("@prompt_tokens", record.PromptTokens),
            new("@completion_tokens", record.CompletionTokens),
            new("@total_tokens", record.TotalTokens),
            new("@input_tokens_cost", record.InputTokensCost.ToString()),
            new("@output_tokens_cost", record.OutputTokensCost.ToString()),
            new("@request_cost", record.RequestCost.ToString()),
            new("@total_cost", record.TotalCost.ToString()),
            new("@request_body", !string.IsNullOrWhiteSpace(record.RequestBody) 
                ? record.RequestBody : (object?)DBNull.Value),
            new("@response_string", !string.IsNullOrWhiteSpace(record.ResponseString) 
                ? record.ResponseString : (object?)DBNull.Value),
            new("@error", !string.IsNullOrWhiteSpace(error) ? error : (object?)DBNull.Value)
            ];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException("Insert into perplexity_api_call failed, no row id returned.");
    }

    public async Task CreateTableAsync()
    {
        string commandText = $@"
            CREATE TABLE IF NOT EXISTS perplexity_api_call (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                news_snapshot_id INTEGER, -- Foreign key to link to news_snapshot table
                created_on TEXT NOT NULL DEFAULT (datetime('now', 'utc')),
                prompt_tokens INTEGER,
                completion_tokens INTEGER,
                total_tokens INTEGER,
                input_tokens_cost TEXT,
                output_tokens_cost TEXT,
                request_cost TEXT,
                total_cost TEXT,
                request_body TEXT,
                response_string TEXT,
                error TEXT,
                FOREIGN KEY(news_snapshot_id) REFERENCES news_snapshot(id) ON DELETE CASCADE);

            INSERT INTO database_info (entity, version) 
                VALUES ('perplexity_api_call', '{Version}');";

        await database.ExecuteNonQueryAsync(commandText);
    }

    public bool UpdateTable(float currentVersion)
    {
        if (currentVersion == 1.1F)
        {
            string commandText = $@"
                ALTER TABLE perplexity_api_call ADD COLUMN request_body TEXT;
                                
                UPDATE database_info SET version = '{currentVersion += .1F}', updated_on = '{DateTime.UtcNow}' 
                WHERE entity = 'perplexity_api_call';";
            database.ExecuteNonQueryAsync(commandText).Wait();
            
            return UpdateTable(currentVersion);
        }
        if (currentVersion == 1.2F)
        {
            string commandText = $@"
                ALTER TABLE perplexity_api_call ADD COLUMN error TEXT;
                
                UPDATE database_info SET version = '{currentVersion += .1F}', updated_on = '{DateTime.UtcNow}'  
                WHERE entity = 'perplexity_api_call';";
            database.ExecuteNonQueryAsync(commandText).Wait();
            return UpdateTable(currentVersion);
        }
        if (currentVersion == 1.3F)
        {
            string commandText = $@"
                ALTER TABLE perplexity_api_call ADD COLUMN request_body TEXT;
                
                UPDATE database_info SET version = '{currentVersion += .1F}', updated_on = '{DateTime.UtcNow}'  
                WHERE entity = 'perplexity_api_call';";
            database.ExecuteNonQueryAsync(commandText).Wait();
            return UpdateTable(currentVersion);
        }

        return true; // Table is up to date
    }
}
