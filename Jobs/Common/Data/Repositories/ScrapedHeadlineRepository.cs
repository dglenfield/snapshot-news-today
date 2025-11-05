using Common.Models;
using Microsoft.Data.Sqlite;

namespace Common.Data.Repositories;

public class ScrapedHeadlineRepository(NewsSnapshotDatabase database)
{
    public async Task CreateTableAsync()
    {
        string script = "CreateScrapedHeadlineTableV1.1";
        string scriptFilePath = Path.Combine(AppContext.BaseDirectory, "Data\\Scripts", script);
        string scriptContent = File.ReadAllText(scriptFilePath);

        await database.ExecuteNonQueryAsync(scriptContent);
    }

    public async Task<long> CreateAsync(ScrapedHeadline headline, long jobId)
    {
        string commandText = @"
            INSERT INTO scraped_headline (
                snapshot_job_id, section_name, headline, target_uri, last_updated_on, most_read) 
            VALUES (@snapshot_job_id, @section_name, @headline, @target_uri, @last_updated_on, @most_read);";

        SqliteParameter[] parameters = [
            new("@snapshot_job_id", jobId),
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
