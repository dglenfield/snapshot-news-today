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
                //Id = article.Id,
                //Category = article.SectionName, 
                Headline = article.Headline,
                LastUpdatedOn = article.LastUpdatedOn, 
                SourceUri = article.SourceUri
            };
            sourceArticles.Add(sourceArticle);
        }

        logger.Log($"Source Articles: {sourceArticles.Count}");

        var topStoryArticles = await provider.SelectArticles(sourceArticles);

        return topStoryArticles;
    }
}
