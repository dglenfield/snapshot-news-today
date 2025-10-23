using Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NewsScraper.Configuration;
using NewsScraper.Configuration.Options;
using NewsScraper.Data;
using NewsScraper.Data.Providers;
using NewsScraper.Enums;
using NewsScraper.Models;
using NewsScraper.Processors;
using NewsScraper.Providers;
using NewsScraper.Scrapers.AssociatedPress.MainPage;

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
        NewsWebsite targetSite = NewsWebsite.AssociatedPress;

        int returnCode = 0;
        Logger logger = null!;
        string logName = $"{targetSite}_{DateTime.Now:yyyy-MM-dd.HHmm.ss}";

        try
        {
            Console.Title = "News Scraper";

            // Configure dependency injection and initialize the host
            var host = CreateBuilder(logName, targetSite).Build();

            var configSettings = host.Services.GetRequiredService<ConfigurationSettings>();
            logger = new Logger(configSettings.CustomLoggingOptions.LogLevel, 
                configSettings.CustomLoggingOptions.LogToFile, 
                configSettings.CustomLoggingOptions.LogDirectory, logName);
            logger.Log("**Host initialized**");

            // Log Configuration Settings
            if (configSettings.ApplicationOptions.LogConfigurationSettings)
                configSettings.WriteToLog();

            // Ensure the SQLite database is created
            var scrapeJobDataProvider = host.Services.GetRequiredService<ScrapeJobDataProvider>();
            await scrapeJobDataProvider.CreateDatabaseAsync();

            ScrapeJob job = new()
            {
                SourceName = targetSite.ToString(),
                SourceUri = targetSite switch
                {
                    NewsWebsite.AssociatedPress => configSettings.NewsSourceOptions.AssociatedPress.BaseUri,
                    NewsWebsite.CNN => configSettings.NewsSourceOptions.CNN.BaseUri,
                    _ => throw new NotImplementedException($"Scraping not implemented for {targetSite}")
                }
            };

            // Resolve and run the main service
            if (targetSite == NewsWebsite.AssociatedPress)
            {
                var options = host.Services.GetRequiredService<IOptions<NewsSourceOptions.AssociatedPressOptions>>().Value;
                job.UseTestFile = options.Scrapers.MainPage.UseTestFile;
                job.TestFile = job.UseTestFile ? options.Scrapers.MainPage.TestFile : null;
                var processor = host.Services.GetRequiredService<AssociatePressProcessor>();
                await processor.Run(job);
            }
            else if (targetSite == NewsWebsite.CNN)
            {
                var processor = host.Services.GetRequiredService<CnnProcessor>();
                await processor.Run();
            }
        }
        catch (Exception ex)
        {
            if (logger is null)
                Console.WriteLine(ex.ToString());
            else
                logger.LogException(ex);
            
            returnCode = 1;
        }

        return returnCode;
    }

    private static IHostBuilder CreateBuilder(string logName, NewsWebsite targetSite)
    {
        // Set up dependency injection
        return Host.CreateDefaultBuilder().ConfigureServices((context, services) => 
        {
            // Register each options class
            services.AddOptions<ApplicationOptions>()
                .BindConfiguration(ApplicationOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();
            services.AddOptions<CustomLoggingOptions>()
                .BindConfiguration(CustomLoggingOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();
            services.AddOptions<DatabaseOptions>()
                .BindConfiguration(DatabaseOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();
            services.AddOptions<PythonOptions>()
                .BindConfiguration(PythonOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();
            services.AddOptions<NewsSourceOptions>()
                .BindConfiguration(NewsSourceOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();
            if (targetSite == NewsWebsite.AssociatedPress)
            {
                services.AddOptions<NewsSourceOptions.AssociatedPressOptions>()
                    .BindConfiguration($"{NewsSourceOptions.SectionName}:{NewsSourceOptions.AssociatedPressOptions.SectionName}")
                    .ValidateDataAnnotations().ValidateOnStart();
            }
            else if (targetSite == NewsWebsite.CNN)
            {
                services.AddOptions<NewsSourceOptions.CnnOptions>()
                    .BindConfiguration(NewsSourceOptions.SectionName)
                    .ValidateDataAnnotations().ValidateOnStart();
            }

            // ConfigurationSettings
            services.AddTransient<ConfigurationSettings>();

            // Logging
            services.AddSingleton<Logger>(provider => new Logger(
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogLevel,
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogToFile,
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogDirectory,
                logName));

            // Data providers and repositories
            services.AddTransient<ScrapeJobDataProvider>();
            services.AddTransient<ScrapeJobRepository>();
            services.AddTransient<NewsArticleRepository>();

            // News website specific services
            if (targetSite == NewsWebsite.AssociatedPress)
            {
                services.AddTransient<AssociatedPressHeadlineRepository>();
                services.AddTransient<AssociatePressProcessor>();
                services.AddTransient<MainPageScraper>();
            }
            else if (targetSite == NewsWebsite.CNN)
            {
                services.AddTransient<CnnArticleProvider>();
            }
        });
    }
}
