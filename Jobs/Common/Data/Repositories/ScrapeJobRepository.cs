//using Common.Models;
//using Microsoft.Data.Sqlite;

//namespace Common.Data.Repositories;

//public class ScrapeJobRepository(NewsSnapshotDatabase database)
//{
//    public async Task CreateTableAsync()
//    {
//        string script = "CreateScrapeJobTableV1.1";
//        string scriptFilePath = Path.Combine(AppContext.BaseDirectory, "Data\\Scripts", script);
//        string scriptContent = File.ReadAllText(scriptFilePath);

//        await database.ExecuteNonQueryAsync(scriptContent);
//    }

//    //public async Task<long> CreateAsync(ScrapeResult job)
//    //{
//    //    string commandText = @"
//    //        INSERT INTO scrape_job (source, job_started_on) 
//    //        VALUES (@source, @scrape_started_on);";

//    //    SqliteParameter[] parameters = [
//    //        new("@source", job.Source),
//    //        new("@scrape_started_on", (object?)job.StartedOn.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value)];

//    //    long id = await database.InsertAsync(commandText, parameters);
//    //    if (id > 0)
//    //        return id;

//    //    throw new InvalidOperationException("Insert into scrape_job failed, no row id returned.");
//    //}

//    //public async Task UpdateAsync(ScrapeResult job)
//    //{
//    //    string jobError = job.ScrapeJobException is not null ? $"{job.ScrapeJobException.Source}: {job.ScrapeJobException.Message}" : string.Empty;
//    //    string mainPageErrors = string.Empty;
//    //    if (job.ScrapeMainPageResult?.ScrapeExceptions is not null)
//    //        foreach (var exception in job.ScrapeMainPageResult.ScrapeExceptions)
//    //            mainPageErrors += $"{exception.Source}: {exception.Message} | ";
//    //    string articlesErrors = string.Empty;
//    //    foreach (var article in job.ScrapedArticles.Where(a => a.ScrapeException is not null))
//    //        articlesErrors += $"{article.ScrapeException?.Source}: {article.ScrapeException?.Message} | ";

//    //    string commandText = @"
//    //        UPDATE scrape_job
//    //        SET sections_scraped = @sections_scraped, 
//    //            headlines_scraped = @headlines_scraped,
//    //            articles_scraped = @articles_scraped,
//    //            job_finished_on = @job_finished_on,
//    //            run_time_in_seconds = @run_time_in_seconds,
//    //            is_success = @is_success, 
//    //            job_error = @job_error,
//    //            main_page_errors = @main_page_errors,
//    //            articles_errors = @articles_errors
//    //        WHERE id = @id;";

//    //    SqliteParameter[] parameters = [
//    //        new("@id", job.Id),
//    //        new("@sections_scraped", (object?)job.ScrapeMainPageResult?.SectionsScraped ?? DBNull.Value),
//    //        new("@headlines_scraped", (object?)job.ScrapeMainPageResult?.HeadlinesScraped ?? DBNull.Value),
//    //        new("@articles_scraped", (object?)job.ScrapedArticles.Count ?? DBNull.Value),
//    //        new("@job_finished_on", (object?)job.FinishedOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value),
//    //        new("@run_time_in_seconds", (object?)job.RunTimeInSeconds ?? DBNull.Value),
//    //        new("@is_success", job.IsSuccess.HasValue ? (job.IsSuccess.Value ? 1 : 0) : (object?)DBNull.Value),
//    //        new("@job_error", !string.IsNullOrWhiteSpace(jobError) ? jobError : (object?)DBNull.Value),
//    //        new("@main_page_errors", !string.IsNullOrWhiteSpace(mainPageErrors) ? mainPageErrors : (object?)DBNull.Value),
//    //        new("@articles_errors", !string.IsNullOrWhiteSpace(articlesErrors) ? articlesErrors : (object?)DBNull.Value)
//    //    ];

//    //    int rowsAffected = await database.ExecuteNonQueryAsync(commandText, parameters);
//    //    if (rowsAffected == 0)
//    //        throw new InvalidOperationException($"No record found with id = {job.Id} to update in the scrape_job table.");
//    //}

    
//}
