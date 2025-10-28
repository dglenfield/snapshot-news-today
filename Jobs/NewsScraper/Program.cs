using Common.Configuration.Options;
using Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NewsScraper.Configuration;
using NewsScraper.Configuration.Options;
using NewsScraper.Data;
using NewsScraper.Data.Repositories;
using NewsScraper.Models.AssociatedPress;
using NewsScraper.Processors;
using NewsScraper.Scrapers.AssociatedPress.ArticlePage;
using NewsScraper.Scrapers.AssociatedPress.MainPage;
using NewsScraper.Scrapers.CNN;

namespace NewsScraper;

/// <summary>
/// Provides the entry point for the News Scraper application.
/// </summary>
/// <remarks>This class contains the application's main method, which initializes logging, configures dependency injection, 
/// and starts the news scraping process. The exit code returned by the main method indicates success (0) or failure (1).</remarks>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        NewsWebsite targetSite = NewsWebsite.AssociatedPress;

        int returnCode = 0;
        Logger logger = null!;
        
        try
        {
            // Configure dependency injection and initialize the host
            var host = CreateBuilder(targetSite, DateTime.UtcNow).Build();

            var configSettings = host.Services.GetRequiredService<ConfigurationSettings>();
            Console.Title = configSettings.ApplicationOptions.Name;

            logger = host.Services.GetRequiredService<Logger>();
            logger.Log("**Host initialized**", LogLevel.Debug);

            // Log Configuration Settings
            if (configSettings.ApplicationOptions.LogConfigurationSettings)
                configSettings.WriteToLog();

            // Create the database if enabled in appsettings
            if (configSettings.DatabaseOptions.CreateDatabase)
            {
                var database = host.Services.GetRequiredService<NewsScraperDatabase>();
                bool databaseCreated = await database.CreateAsync();
                logger.Log($"Database {(databaseCreated ? "created" : "already exists")} at '{database.DatabaseFilePath}'.", LogLevel.Success);
            }

            // Resolve and run the main service
            switch (targetSite)
            {
                case NewsWebsite.AssociatedPress:
                    var articlePageConfig = configSettings.NewsSourceOptions.AssociatedPress.Scrapers.ArticlePage;
                    var mainPageConfig = configSettings.NewsSourceOptions.AssociatedPress.Scrapers.MainPage;
                    ScrapeJob job = new()
                    {
                        SourceName = targetSite.ToString(),
                        SourceUri = configSettings.NewsSourceOptions.AssociatedPress.BaseUri,
                        SkipArticlePageScrape = configSettings.ApplicationOptions.UseProductionSettings ? false : articlePageConfig.Skip,
                        SkipMainPageScrape = configSettings.ApplicationOptions.UseProductionSettings ? false : mainPageConfig.Skip,
                        UseArticlePageTestFile = configSettings.ApplicationOptions.UseProductionSettings ? false : articlePageConfig.UseTestFile,
                        UseMainPageTestFile = configSettings.ApplicationOptions.UseProductionSettings ? false : mainPageConfig.UseTestFile
                    };
                    job.ArticlePageTestFile = job.UseArticlePageTestFile ? articlePageConfig.TestFile : null;
                    job.MainPageTestFile = job.UseMainPageTestFile ? mainPageConfig.TestFile : null;
                    var apNewsProcessor = host.Services.GetRequiredService<APNewsProcessor>();
                    await apNewsProcessor.Run(job);
                    break;
                case NewsWebsite.CNN:
                    var cnnProcessor = host.Services.GetRequiredService<CnnProcessor>();
                    await cnnProcessor.Run();
                    break;
                default: 
                    throw new NotImplementedException($"Scraping not implemented for {targetSite}");
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

    private static IHostBuilder CreateBuilder(NewsWebsite targetSite, DateTime logTimestamp)
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

            // ConfigurationSettings
            services.AddTransient<ConfigurationSettings>();

            // Logging
            services.AddSingleton(provider => new Logger(
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogLevel,
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogToFile,
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogDirectory,
                $"{targetSite}_{logTimestamp:yyyy-MM-ddTHHmm.ssZ}"));

            // Database and repositories
            services.AddTransient(provider => new NewsScraperDatabase(
                provider.GetRequiredService<IOptions<DatabaseOptions>>()));
            services.AddTransient<APNewsScrapeJobRepository>();
            services.AddTransient<APNewsHeadlineRepository>();
            services.AddTransient<APNewsArticleRepository>();

            // News website specific services
            switch (targetSite)
            {
                case NewsWebsite.AssociatedPress:
                    services.AddOptions<NewsSourceOptions.AssociatedPressOptions>()
                        .BindConfiguration($"{NewsSourceOptions.SectionName}:{NewsSourceOptions.AssociatedPressOptions.SectionName}")
                        .ValidateDataAnnotations().ValidateOnStart();
                    services.AddTransient<APNewsProcessor>();
                    services.AddTransient<MainPageScraper>();
                    services.AddTransient<ArticlePageScraper>();
                    break;
                case NewsWebsite.CNN:
                    services.AddOptions<NewsSourceOptions.CnnOptions>()
                        .BindConfiguration(NewsSourceOptions.SectionName)
                        .ValidateDataAnnotations().ValidateOnStart();
                    services.AddTransient<CnnArticleProvider>();
                    break;
            }
        });
    }
}
