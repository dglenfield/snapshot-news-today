using Common;
using Common.Configuration.Options;
using Common.Data;
using Common.Data.Repositories;
using Common.Logging;
using Common.Models;
using Common.Models.AssociatedPress;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NewsSnapshot.Configuration;
using NewsSnapshot.Configuration.Options;
using NewsSnapshot.Processors;
using NewsSnapshot.Scrapers.AssociatedPress.ArticlePage;
using NewsSnapshot.Scrapers.AssociatedPress.MainPage;

namespace NewsSnapshot;

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

            // Initialize the database
            var database = host.Services.GetRequiredService<NewsSnapshotDatabase>();
            await database.InitializeAsync();

            // Resolve and run the main service            
            APNewsScrape apNewsScrape = new() 
            { 
                SourceName = targetSite.ToString(), 
                SourceUri = configSettings.NewsSourceOptions.AssociatedPress.BaseUri
            };
            NewsSnapshotJob job = new() 
            {
                APNewsScrape = apNewsScrape
            };
            var newsSnapshotProcessor = host.Services.GetRequiredService<NewsSnapshotProcessor>();
            await newsSnapshotProcessor.Run(job);

            //switch (targetSite)
            //{
            //    case NewsWebsite.AssociatedPress:
            //        var articlePageConfig = configSettings.NewsSourceOptions.AssociatedPress.Scrapers.ArticlePage;
            //        var mainPageConfig = configSettings.NewsSourceOptions.AssociatedPress.Scrapers.MainPage;
            //        APNewsScrape job = new()
            //        {
            //            SourceName = targetSite.ToString(),
            //            SourceUri = configSettings.NewsSourceOptions.AssociatedPress.BaseUri,
            //            SkipArticlePageScrape = configSettings.ApplicationOptions.UseProductionSettings ? false : articlePageConfig.Skip,
            //            SkipMainPageScrape = configSettings.ApplicationOptions.UseProductionSettings ? false : mainPageConfig.Skip,
            //            UseArticlePageTestFile = configSettings.ApplicationOptions.UseProductionSettings ? false : articlePageConfig.UseTestFile,
            //            UseMainPageTestFile = configSettings.ApplicationOptions.UseProductionSettings ? false : mainPageConfig.UseTestFile
            //        };
            //        job.ArticlePageTestFile = job.UseArticlePageTestFile ? articlePageConfig.TestFile : null;
            //        job.MainPageTestFile = job.UseMainPageTestFile ? mainPageConfig.TestFile : null;
            //        var apNewsProcessor = host.Services.GetRequiredService<APNewsProcessor>();
            //        await apNewsProcessor.Run(job);
            //        break;
            //    case NewsWebsite.CNN:
            //        var cnnProcessor = host.Services.GetRequiredService<CnnProcessor>();
            //        await cnnProcessor.Run();
            //        break;
            //    default:
            //        throw new NotImplementedException($"Scraping not implemented for {targetSite}");
            //}

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
            services.AddOptions<NewsSourceOptions>()
                .BindConfiguration(NewsSourceOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();
            services.AddOptions<PerplexityOptions>()
                .BindConfiguration(PerplexityOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();
            services.AddOptions<PythonOptions>()
                .BindConfiguration(PythonOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();

            // ConfigurationSettings
            services.AddTransient<ConfigurationSettings>();

            // Logging
            services.AddSingleton(provider => new Logger(
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogLevel,
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogToFile,
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogDirectory,
                $"NewsSnapshot_{logTimestamp:yyyy-MM-ddTHHmm.ssZ}"));

            // Database and repositories
            services.AddTransient<NewsSnapshotDatabase>();
            services.AddTransient<NewsSnapshotJobRepository>();
            services.AddTransient<APNewsScrapeRepository>();
            services.AddTransient<APNewsHeadlineRepository>();
            services.AddTransient<APNewsArticleRepository>();

            // Processors
            services.AddTransient<NewsSnapshotProcessor>();
            services.AddTransient<APNewsProcessor>();

            services.AddHttpClient("Perplexity", (serviceProvider, client) =>
            {
                var perplexityOptions = serviceProvider.GetRequiredService<IOptions<PerplexityOptions>>().Value;

                client.BaseAddress = perplexityOptions.ApiUri;
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {perplexityOptions.ApiKey}");
            });

            // News website specific services
            switch (targetSite)
            {
                case NewsWebsite.AssociatedPress:
                    services.AddOptions<NewsSourceOptions.AssociatedPressOptions>()
                        .BindConfiguration($"{NewsSourceOptions.SectionName}:{NewsSourceOptions.AssociatedPressOptions.SectionName}")
                        .ValidateDataAnnotations().ValidateOnStart();
                    //services.AddTransient<APNewsProcessor>();
                    services.AddTransient<MainPageScraper>();
                    services.AddTransient<ArticlePageScraper>();
                    break;
                case NewsWebsite.CNN:
                    services.AddOptions<NewsSourceOptions.CnnOptions>()
                        .BindConfiguration(NewsSourceOptions.SectionName)
                        .ValidateDataAnnotations().ValidateOnStart();
                    //services.AddTransient<CnnArticleProvider>();
                    break;
            }
        });
    }
}