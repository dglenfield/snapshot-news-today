using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewsScraper.Logging;
using NewsScraper.Providers;

namespace NewsScraper;

public class Program
{
    private static readonly NewsProvider _newsProvider = default!;
    private static readonly PerplexityApiProvider _perplexityApiProvider = default!;

    public static void Main(string[] args)
    {
        try
        {
            // Get current news articles from CNN
            NewsWebsite targetSite = NewsWebsite.CNN;
            var sourceArticles = _newsProvider.GetNewsArticles(targetSite);
            if (sourceArticles.Count == 0)
                ExitApplication($"No articles found from {targetSite}.", LogLevel.Warning);

            Logger.Log($"Total articles retrieved from {targetSite}: {sourceArticles.Count}");
            foreach (var article in sourceArticles)
                Logger.Log(article.SourceUri.AbsoluteUri.ToString(), logAsRawMessage: true);

            // Curate articles using Perplexity API
            var curatedNewsArticles = _perplexityApiProvider.CurateArticles([.. sourceArticles]).GetAwaiter().GetResult();

            // Analyze each curated article (not implemented)


            ExitApplication();
        }
        catch (Exception ex)
        {
            ExitWithError(ex, 1);
        }
    }

    private static void ExitApplication(string message = "", LogLevel messageLogLevel = LogLevel.Info, int exitCode = 0)
    {
        try
        {
            if (!string.IsNullOrEmpty(message))
                Logger.Log(message, messageLogLevel);
            Logger.Log("\n********** Exiting application **********");
        }
        catch (Exception)
        {
            Console.Error.WriteLine("\nUnable to write to log file. Exiting application.\n");
        }
        
        Environment.Exit(exitCode);
    }

    private static void ExitWithError(Exception ex, int exitCode) => 
        ExitApplication($"{ex.GetType()}: {ex.Message}\nStackTrace: {ex.StackTrace}", LogLevel.Error, exitCode);

    static Program()
    {
        try
        {
            Console.Title = "News Scraper";
            Logger.Log("********** Application started **********");

            // Setup DI
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient("Perplexity", client =>
                    {
                        client.BaseAddress = new Uri(Configuration.PerplexityApiUrl);
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Configuration.PerplexityApiKey}");
                    });
                    services.AddTransient<NewsProvider>();
                    services.AddTransient<PerplexityApiProvider>();
                }).Build();

            _newsProvider = host.Services.GetRequiredService<NewsProvider>();
            _perplexityApiProvider = host.Services.GetRequiredService<PerplexityApiProvider>();
        }
        catch (TypeInitializationException ex)
        {
            ExitWithError(ex, 2);
        }
        catch (Exception ex)
        {
            ExitWithError(ex, 3);
        }
    }
}
