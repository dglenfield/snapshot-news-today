using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewsScraper.Logging;
using NewsScraper.Providers;

namespace NewsScraper;

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
    private static readonly NewsProvider _newsProvider = default!;
    private static readonly PerplexityApiProvider _perplexityApiProvider = default!;

    public static int Main(string[] args)
    {
        try
        {
            // TODO: Move getting news and processing into a processor class
            // Get current news articles from CNN
            NewsWebsite targetSite = NewsWebsite.CNN;
            var sourceArticles = _newsProvider.GetNewsArticles(targetSite);
            if (sourceArticles.Count == 0)
            {
                Logger.Log($"No articles found from {targetSite}.", LogLevel.Warning);
                return 0;
            }

            // Log retrieved articles (if not using test data)
            if (!Configuration.TestSettings.NewsProvider.GetNews.UseTestLandingPageFile)
            {
                Logger.Log($"Total articles retrieved from {targetSite}: {sourceArticles.Count}");
                foreach (var article in sourceArticles)
                    Logger.Log(article.SourceUri.AbsoluteUri.ToString(), logAsRawMessage: true);
            }
            // END TODO: Move the above into a processor class

            // TODO: Move curating and analyzing articles into a processor class
            // Curate articles using Perplexity API
            var curatedNewsArticles = _perplexityApiProvider.CurateArticles([.. sourceArticles]).GetAwaiter().GetResult();

            // Log curated articles
            foreach (var article in curatedNewsArticles.Articles)
                Logger.Log($"\n{article}", logAsRawMessage: true);
            // END TODO: Move the above into a processor class

            // Analyze each curated article (not implemented)
            // 1. Create a GetNewsArticle method in NewsProvider to get full article details by URL
            // 2. Call PerplexityApiProvider.AnalyzeArticle for each curated article (or batch them if possible)

            return 0;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            return 1;
        }
    }

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
            Logger.LogException(ex);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            throw;
        }
    }
}
