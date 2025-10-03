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
            Console.Title = "News Scraper Application";
            Logger.Log("********** Application started **********");
            Configuration.LogConfigurationSettings();

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
            Environment.Exit(1);
        }
    }

    public static void Main(string[] args)
    {
        try
        {
            // 1. Fetch article URLs from news website
            NewsWebsite targetSite = NewsWebsite.CNN;
            var distinctUris = NewsProvider.GetNews(targetSite);

            // 2. Curate article URLs using Perplexity API
            if (distinctUris is null || distinctUris.Count == 0)
            {
                Logger.Log("No article URIs found to curate.", LogLevel.Warning);
                Logger.Log("********** Exiting application **********");
                Environment.Exit(0);
            }

            foreach ( var uri in distinctUris ) { Logger.Log(uri.AbsoluteUri, logAsRawMessage: true); }

            //_perplexityApiProvider.CurateArticles([.. distinctUris]).GetAwaiter().GetResult();

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
