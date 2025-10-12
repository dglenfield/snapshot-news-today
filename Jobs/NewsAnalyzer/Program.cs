using Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewsAnalyzer.Processors;
using NewsAnalyzer.Providers;

namespace NewsAnalyzer;

public class Program
{
    public static int Main(string[] args)
    {
        Console.Title = "News Scraper";
        var logger = new Logger(Configuration.Logging.LogLevel, Configuration.Logging.LogDirectory, Configuration.Logging.LogToFile);
        int returnCode = 0;

        try
        {
            logger.Log("********** Application started **********");

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<Logger>(provider =>
                        new Logger(
                            Configuration.Logging.LogLevel,
                            Configuration.Logging.LogDirectory,
                            Configuration.Logging.LogToFile
                        ));
                    services.AddHttpClient("Perplexity", client =>
                    {
                        client.BaseAddress = new Uri(Configuration.PerplexityApiUrl);
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Configuration.PerplexityApiKey}");
                    });
                    services.AddTransient<NewsAnalyzerProcessor>();
                    services.AddTransient<PerplexityApiProvider>();
                }).Build();

            // Resolve and run your main service
            var processor = host.Services.GetRequiredService<NewsAnalyzerProcessor>();
            processor.Run();
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
