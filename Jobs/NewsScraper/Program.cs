using Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewsScraper.Data;
using NewsScraper.Data.Providers;
using NewsScraper.Enums;
using NewsScraper.Models;
using NewsScraper.Processors;
using NewsScraper.Providers;
using NewsScraper.Scrapers;
using NewsScraper.Scrapers.AssociatedPress.MainPage;
using DbSettings = NewsScraper.Configuration.Database;
using LogSettings = NewsScraper.Configuration.Logging;

namespace NewsScraper;

/// <summary>
/// Provides the entry point for the News Scraper application.
/// </summary>
/// <remarks>This class contains the application's main method, which initializes logging, configures dependency injection, 
/// and starts the news scraping process. The exit code returned by the main method indicates success (0) or failure (1).</remarks>
public class Program
{
    /// <summary>
    /// Initializes and runs the News Scraper application using the specified command-line arguments.
    /// </summary>
    /// <remarks>This method serves as the application's entry point and is typically called by the runtime. 
    /// The exit code can be used by external processes to determine whether the application ran successfully.</remarks>
    /// <param name="args">An array of command-line arguments supplied to the application. These may be used to configure application
    /// behavior.</param>
    /// <returns>An integer exit code indicating the result of the application's execution. Returns 0 if the application
    /// completes successfully; otherwise, returns 1 if an unhandled exception occurs.</returns>
    public static async Task<int> Main(string[] args)
    {
        int returnCode = 0;
        Logger logger = null!;

        try
        {
            Console.Title = "News Scraper";
            NewsWebsite targetSite = NewsWebsite.AssociatedPress;
            ScrapeJob.SourceName = targetSite.ToString();
            ScrapeJob.SourceUri = targetSite switch
            {
                NewsWebsite.AssociatedPress => new Uri(Configuration.NewsSourceUrls.AssociatedPressBaseUrl),
                NewsWebsite.CNN => new Uri(Configuration.NewsSourceUrls.CnnBaseUrl),
                _ => throw new NotImplementedException($"Scraping not implemented for {targetSite}"),
            };
            string logName = $"{targetSite}_{DateTime.Now:yyyy-MM-dd.HHmm.ss}";
            logger = new Logger(LogSettings.LogLevel, LogSettings.LogDirectory, logName);
            logger.Log("********** Application started **********");
            if (Configuration.LogConfigurationSettings)
                logger.Log($"Configuration Settings:\n{Configuration.ToJson()}", logAsRawMessage: true);

            // Set up dependency injection
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Logging
                    services.AddSingleton<Logger>(provider 
                        => new Logger(LogSettings.LogLevel, LogSettings.LogDirectory, logName));
                    // Data providers and repositories
                    services.AddTransient<ScrapeJobDataProvider>(provider 
                        => new ScrapeJobDataProvider(
                            DbSettings.NewsScraperJob.DatabaseFilePath, DbSettings.NewsScraperJob.DatabaseVersion,
                            provider.GetRequiredService<Logger>()));
                    services.AddTransient<ScrapeJobRepository>();
                    services.AddTransient<NewsArticleRepository>();
                    // Processors and other providers
                    if (targetSite == NewsWebsite.AssociatedPress)
                    {
                        services.AddTransient<AssociatePressProcessor>();
                        services.AddTransient<MainPageScraper>();
                    }
                    if (targetSite == NewsWebsite.CNN)
                    {
                        services.AddTransient<CnnArticleProvider>(provider 
                            => new CnnArticleProvider(
                                Configuration.NewsSourceUrls.CnnBaseUrl, Configuration.PythonSettings.PythonExePath,
                                provider.GetRequiredService<Logger>()));
                    }
                        
                }).Build();

            // Ensure the SQLite database is created
            var scrapeJobDataProvider = host.Services.GetRequiredService<ScrapeJobDataProvider>();
            await scrapeJobDataProvider.CreateDatabaseAsync();

            // Resolve and run the main service
            if (targetSite == NewsWebsite.AssociatedPress)
            {
                var processor = host.Services.GetRequiredService<AssociatePressProcessor>();
                await processor.Run();
            }
            else if (targetSite == NewsWebsite.CNN)
            {
                var processor = host.Services.GetRequiredService<CnnProcessor>();
                await processor.Run();
            }
                
        }
        catch (Exception ex)
        {
            logger ??= new(LogSettings.LogLevel, LogSettings.LogDirectory);
            logger.LogException(ex);
            returnCode = 1;
        }

        logger.Log("********** Exiting application **********");
        return returnCode;
    }
}
