using Common.Logging;
using Microsoft.Data.Sqlite;
using NewsScraper.Data.Providers;
using NewsScraper.Models.AssociatedPress.MainPage;
using System.Data.Common;

namespace NewsScraper.Data;

internal class AssociatedPressHeadlineRepository(ScrapeJobDataProvider dataProvider, Logger logger)
{
    public string DatabaseFilePath => dataProvider.DatabaseFilePath;

    public async Task<long> CreateAssociatedPressHeadlineAsync(Headline headline, long scrapeJobId)
    {
        string commandText = @"
            INSERT INTO associated_press_headline (
                scrape_job_id, section_name, headline, target_uri, last_updated_on, published_on, most_read) 
            VALUES (@scrape_job_id, @section_name, @headline, @target_uri, @last_updated_on, @published_on, @most_read);";
        SqliteParameter[] parameters = [
            new("@scrape_job_id", scrapeJobId),
            new("@section_name", headline.SectionName),
            new("@headline", headline.Title),
            new("@target_uri", (object)headline.TargetUri.AbsoluteUri),
            new("@last_updated_on", (object?)headline.LastUpdatedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@published_on", (object?)headline.PublishedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
            new("@most_read", headline.MostRead ? 1 : 0)
        ];
        try
        {
            long id = await dataProvider.InsertAsync(commandText, parameters);
            return id > 0 ? id : throw new InvalidOperationException("Insert associated_press_headline failed, no row id returned.");
        }
        catch (DbException)
        {
            logger.Log("Error inserting record into associated_press_headline.", LogLevel.Error);
            throw;
        }
    }
}
