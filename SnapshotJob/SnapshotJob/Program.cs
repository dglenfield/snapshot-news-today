using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SnapshotJob.Common.Logging;
using SnapshotJob.Configuration;
using SnapshotJob.Configuration.Options;
using SnapshotJob.Data;
using SnapshotJob.Data.Configuration.Options;
using SnapshotJob.Data.Repositories;
using SnapshotJob.Perplexity;
using SnapshotJob.Perplexity.Configuration.Options;
using SnapshotJob.Processors;
using SnapshotJob.Scrapers.ArticlePage;
using SnapshotJob.Scrapers.MainPage;
using SnapshotNewsToday.Data;
using SnapshotNewsToday.Data.Configuration.Options;

namespace SnapshotJob;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        int returnCode = 0;
        Logger logger = null!;

        try
        {
            // Configure dependency injection and initialize the host
            var host = CreateBuilder(DateTime.UtcNow).Build();

            var configSettings = host.Services.GetRequiredService<ConfigurationSettings>();
            Console.Title = configSettings.ApplicationOptions.Name;

            logger = host.Services.GetRequiredService<Logger>();
            logger.Log("**Host initialized**", LogLevel.Debug);

            // Log Configuration Settings
            if (configSettings.ApplicationOptions.LogConfigurationSettings)
                configSettings.WriteToLog();

            // Initialize the job database
            var jobDatabase = host.Services.GetRequiredService<SnapshotJobDatabase>();
            await jobDatabase.InitializeAsync();

            // Initialize the application database
            var applicationDatabase = host.Services.GetRequiredService<SnapshotNewsTodayDatabase>();
            //await applicationDatabase.CreateDatabase();
            await applicationDatabase.CreateArticlesContainer();

            // Resolve and run the main service
            var snapshotProcessor = host.Services.GetRequiredService<SnapshotJobProcessor>();
            await snapshotProcessor.Run();

            // Testing - Delete the application database
            //await applicationDatabase.DeleteDatabase();
            await applicationDatabase.DeleteArticlesContainer();
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

    private static IHostBuilder CreateBuilder(DateTime logTimestamp)
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
            services.AddOptions<SnapshotJobDatabaseOptions>()
                .BindConfiguration(SnapshotJobDatabaseOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();
            services.AddOptions<SnapshotNewsTodayDatabaseOptions>()
                .BindConfiguration(SnapshotNewsTodayDatabaseOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();
            services.AddOptions<PerplexityOptions>()
                .BindConfiguration(PerplexityOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();
            services.AddOptions<ScrapingOptions>()
                .BindConfiguration(ScrapingOptions.SectionName)
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
            services.AddTransient<SnapshotJobDatabase>();
            services.AddTransient<NewsSnapshotRepository>();
            services.AddTransient<ScrapedHeadlineRepository>();
            services.AddTransient<ScrapedArticleRepository>();
            services.AddTransient<PerplexityApiCallRepository>();
            services.AddTransient<TopStoryRepository>();
            services.AddTransient<AnalyzedArticleRepository>();

            // Processors
            services.AddTransient<SnapshotJobProcessor>();
            services.AddTransient<ScrapeProcessor>();
            services.AddTransient<TopStoriesProcessor>();

            // Scrapers
            services.AddTransient<MainPageScraper>();
            services.AddTransient<ArticlePageScraper>();

            services.AddHttpClient("Perplexity", (serviceProvider, client) =>
            {
                var perplexityOptions = serviceProvider.GetRequiredService<IOptions<PerplexityOptions>>().Value;

                client.BaseAddress = perplexityOptions.ApiUri;
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {perplexityOptions.ApiKey}");
            });

            services.AddTransient<TopStoriesProvider>();
            services.AddTransient<ArticleProvider>();

            // Azure Cosmos DB configuration
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                var databaseOptions = serviceProvider.GetRequiredService<IOptions<SnapshotNewsTodayDatabaseOptions>>().Value;
                options.UseCosmos(
                    accountEndpoint: databaseOptions.AccountEndpoint,
                    accountKey: databaseOptions.AccountKey,
                    databaseName: databaseOptions.DatabaseId);
            });

            services.AddTransient<SnapshotNewsTodayDatabase>();
        });
    }
}