using Common.Configuration.Options;
using Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NewsAnalyzer.Configuration;
using NewsAnalyzer.Configuration.Options;
using NewsAnalyzer.Data;
using NewsAnalyzer.Processors;
using NewsAnalyzer.Providers;

namespace NewsAnalyzer;

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

            // Create the database if enabled in appsettings
            if (configSettings.DatabaseOptions.CreateDatabase)
            {
                var database = host.Services.GetRequiredService<NewsAnalyzerDatabase>();
                bool databaseCreated = await database.CreateAsync();
                logger.Log($"Database {(databaseCreated ? "created" : "already exists")} at '{database.DatabaseFilePath}'.", LogLevel.Success);
            }

            // Resolve and run the main service
            var processor = host.Services.GetRequiredService<NewsProcessor>();
            await processor.Run();
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
        return Host.CreateDefaultBuilder().ConfigureAppConfiguration((context, config) =>
        {
            // Explicitly add User Secrets
            config.AddUserSecrets("8f1e9ee3-cac2-4a91-b839-8b3d4bb5c46f");
        }).ConfigureServices((context, services) => 
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
            services.AddOptions<PerplexityOptions>()
                .BindConfiguration(PerplexityOptions.SectionName)
                .ValidateDataAnnotations().ValidateOnStart();

            // ConfigurationSettings
            services.AddTransient<ConfigurationSettings>();

            // Logging
            string timestamp = $"{logTimestamp:yyyy-MM-ddTHHmm.ssZ}";
            services.AddSingleton(provider => new Logger(
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogLevel,
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogToFile,
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogDirectory,
                $"{provider.GetRequiredService<IOptions<ApplicationOptions>>().Value.Name.Replace(" ", "")}_{timestamp}"));

            // Database and repositories
            services.AddTransient(provider => new NewsAnalyzerDatabase(
                provider.GetRequiredService<IOptions<DatabaseOptions>>()));

            services.AddHttpClient("Perplexity", (serviceProvider, client) =>
            {
                var perplexityOptions = serviceProvider.GetRequiredService<IOptions<PerplexityOptions>>().Value;
                
                client.BaseAddress = perplexityOptions.ApiUri;
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {perplexityOptions.ApiKey}");
            });

            services.AddSingleton<NewsProcessor>();
            services.AddTransient<PerplexityApiProvider>();
        });
    }
}
