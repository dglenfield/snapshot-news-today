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
                Id = article.Id.ToString(),
                LastUpdatedOn = article.LastUpdatedOn, 
                SourceUri = article.SourceUri
            };
            sourceArticles.Add(sourceArticle);
        }

        string file = "C:\\Users\\danny\\OneDrive\\Projects\\SnapshotNewsToday\\TestData\\top-stories-response_2025-11-18.json";
        var topStoryArticles = await provider.SelectArticles(sourceArticles, file);
        //var topStoryArticles = await provider.SelectArticles(sourceArticles);

        logger.Log(topStoryArticles.ToString());

        return topStoryArticles;
    }
}
