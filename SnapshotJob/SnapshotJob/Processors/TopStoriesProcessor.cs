using SnapshotJob.Common.Logging;
using SnapshotJob.Data.Models;
using SnapshotJob.Perplexity;
using SnapshotJob.Perplexity.Models;

namespace SnapshotJob.Processors;

internal class TopStoriesProcessor(TopStoriesProvider provider, Logger logger)
{
    internal async Task<TopStoryArticles> SelectArticles(List<ScrapedArticle> scrapedArticles)
    {
        List<SourceNewsArticle> sourceArticles = [];
        foreach (var article in scrapedArticles.OrderByDescending(s => s.LastUpdatedOn).Take(20))
        {
            SourceNewsArticle sourceArticle = new()
            {
                Headline = article.Headline,
                LastUpdatedOn = article.LastUpdatedOn, 
                SourceUri = article.SourceUri
            };
            sourceArticles.Add(sourceArticle);
        }

        string file = "C:\\Users\\danny\\OneDrive\\Projects\\SnapshotNewsToday\\TestData\\curate-articles-response_2025-10-06.json";
        var topStoryArticles = await provider.SelectArticles(sourceArticles, file);
        return topStoryArticles;
    }
}
