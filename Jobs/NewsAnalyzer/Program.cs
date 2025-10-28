using Common.Configuration;
using Common.Configuration.Options;
using Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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

            // Resolve and run the main service
            var processor = host.Services.GetRequiredService<NewsAnalyzerProcessor>();
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

            // ConfigurationSettings
            services.AddTransient<ConfigurationSettings>();

            // Logging
            services.AddSingleton(provider => new Logger(
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogLevel,
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogToFile,
                provider.GetRequiredService<IOptions<CustomLoggingOptions>>().Value.LogDirectory,
                $"{provider.GetRequiredService<IOptions<ApplicationOptions>>().Value.Name.Replace(" ", "")}_{logTimestamp:yyyy-MM-ddTHHmm.ssZ}"));

            services.AddHttpClient("Perplexity", client =>
            {
                client.BaseAddress = new Uri(Configuration.PerplexityApiUrl);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Configuration.PerplexityApiKey}");
            });

            services.AddSingleton<NewsAnalyzerProcessor>();
            services.AddTransient<PerplexityApiProvider>();
        });
    }
}
