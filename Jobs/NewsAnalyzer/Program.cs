using Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewsAnalyzer.Providers;

namespace NewsAnalyzer;

// New workflow:
// Scrape news articles and save to local storage and Azure Cosmos DB,
// then curate and analyze using Perplexity API.
// 1. Get news articles from NewsProvider (e.g., CNN)
// 2. Get full article details for each article
// 3. Save articles to local storage
// 4. Save articles to Azure Cosmos DB
// 5. Analyze articles using Perplexity API

public class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<Logger>(provider =>
                        new Logger(
                            Configuration.LoggingSettings.ApplicationLogLevel,
                            Configuration.LoggingSettings.LogDirectory,
                            Configuration.LoggingSettings.LogToFile
                        ));
                    services.AddHttpClient("Perplexity", client =>
                    {
                        client.BaseAddress = new Uri(Configuration.PerplexityApiUrl);
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Configuration.PerplexityApiKey}");
                    });
                    services.AddTransient<NewsProcessor>();
                    services.AddTransient<PerplexityApiProvider>();
                }).Build();

            // Resolve and run your main service
            var processor = host.Services.GetRequiredService<NewsProcessor>();
            processor.Run(args); // Pass args if needed

            return 0;
        }
        catch (Exception ex)
        {
            new Logger(Configuration.LoggingSettings.ApplicationLogLevel,
                Configuration.LoggingSettings.LogDirectory,
                Configuration.LoggingSettings.LogToFile).LogException(ex);
            return 1;
        }
    }
}
