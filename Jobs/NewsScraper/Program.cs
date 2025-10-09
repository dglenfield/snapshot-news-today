using Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewsScraper.Processors;
using NewsScraper.Providers;

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
        Console.Title = "News Scraper";
        var logger = new Logger(Configuration.Logging.ApplicationLogLevel, Configuration.Logging.LogDirectory, Configuration.Logging.LogToFile);
        int returnCode = 0;

        try
        {
            logger.Log("********** Application started **********");

            // Set up dependency injection
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<Logger>(provider =>
                        new Logger(
                            Configuration.Logging.ApplicationLogLevel,
                            Configuration.Logging.LogDirectory,
                            Configuration.Logging.LogToFile));
                    services.AddTransient<NewsProvider>();
                    services.AddTransient<ScrapingProcessor>();
                    services.AddTransient<SqliteDataProvider>(provider =>
                        new SqliteDataProvider(
                            Configuration.Database.Sqlite.DatabaseFilePath,
                            Configuration.Database.Sqlite.DatabaseVersion,
                            provider.GetRequiredService<Logger>()));
                }).Build();

            // Resolve and run the main service
            var processor = host.Services.GetRequiredService<ScrapingProcessor>();
            await processor.Run();
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            returnCode = 1;
        }

        logger.Log("********** Exiting application **********");
        return returnCode;
    }
}
