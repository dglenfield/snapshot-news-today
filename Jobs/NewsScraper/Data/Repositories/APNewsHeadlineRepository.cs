using Microsoft.Data.Sqlite;
using NewsScraper.Models.AssociatedPress.MainPage;

namespace NewsScraper.Data.Repositories;

internal class APNewsHeadlineRepository(NewsScraperDatabase database)
{
    public async Task<long> CreateAsync(Headline headline, long jobId)
    {
        string commandText = @"
            INSERT INTO ap_news_headline (
                job_id, section_name, headline, target_uri, last_updated_on, most_read) 
            VALUES (@job_id, @section_name, @headline, @target_uri, @last_updated_on, @most_read);";

        SqliteParameter[] parameters = [
            new("@job_id", jobId),
            new("@section_name", (object?)headline.SectionName ?? DBNull.Value),
            new("@headline", headline.Title),
            new("@target_uri", headline.TargetUri.AbsoluteUri),
            new("@last_updated_on", (object?)headline.LastUpdatedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@most_read", headline.MostRead ? 1 : 0)
        ];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException("Insert into ap_news_headline failed, no row id returned.");
    }

    public async Task<long> CreateWithRetryAsync(Headline headline, long scrapeJobId)
    {
        string commandText = @"
            INSERT INTO ap_news_headline (
                scrape_job_id, section_name, headline, target_uri, last_updated_on, published_on, most_read) 
            VALUES (@scrape_job_id, @section_name, @headline, @target_uri, @last_updated_on, @published_on, @most_read);";

        SqliteParameter[] parameters = [
            new("@scrape_job_id", scrapeJobId),
            new("@section_name", headline.SectionName),
            new("@headline", headline.Title),
            new("@target_uri", headline.TargetUri.AbsoluteUri),
            new("@last_updated_on", (object?)headline.LastUpdatedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@most_read", headline.MostRead ? 1 : 0)
        ];
        try
        {
            long id = await RetryAsync(() => database.InsertAsync(commandText, parameters), maxRetries: 3);
            return id > 0 ? id : throw new InvalidOperationException("Insert associated_press_headline failed, no row id returned.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 8)
        {
            //logger.Log($"Database write failed after retries: {ex.Message}", LogLevel.Error);
            throw;
        }
    }

    private async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxRetries)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try { return await operation(); }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 8 && i < maxRetries - 1)
            {
                await Task.Delay(100 * (i + 1)); // 100ms, 200ms, 300ms
            }
        }
        return await operation(); // Final attempt
    }
}
