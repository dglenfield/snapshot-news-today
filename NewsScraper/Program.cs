using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewsScraper.Logging;
using NewsScraper.Providers;

namespace NewsScraper;

public class Program
{
    private static readonly PerplexityApiProvider _perplexityApiProvider;

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
                    services.AddTransient<PerplexityApiProvider>();
                }).Build();
            
            _perplexityApiProvider = host.Services.GetRequiredService<PerplexityApiProvider>();
        }
        catch (TypeInitializationException)
        {
            Console.WriteLine("********** Exiting application **********");
            Environment.Exit(2);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            Logger.Log("********** Exiting application **********");
            Environment.Exit(3);
        }
    }

    public static void Main(string[] args)
    {
        try
        {
            // Fetch articles from CNN
            NewsWebsite targetSite = NewsWebsite.CNN;
            var sourceArticles = NewsProvider.GetNewsArticles(targetSite);
            if (sourceArticles.Count == 0)
            {
                Logger.Log($"No articles found from {targetSite}.", LogLevel.Warning);
                Logger.Log("********** Exiting application **********");
                Environment.Exit(0);
            }
            
            // Curate articles using Perplexity API
            _perplexityApiProvider.CurateArticles([.. sourceArticles]).GetAwaiter().GetResult();

            Logger.Log("********** Exiting application **********");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            Logger.Log("********** Exiting application **********");
            Environment.Exit(1);
        }
    }
}
