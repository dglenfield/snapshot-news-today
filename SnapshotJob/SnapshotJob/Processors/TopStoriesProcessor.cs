using SnapshotJob.Common.Logging;
using SnapshotJob.Data.Models;
using SnapshotJob.Perplexity;
using SnapshotJob.Perplexity.Models.TopStories;

namespace SnapshotJob.Processors;

internal class TopStoriesProcessor(TopStoriesProvider provider, Logger logger)
{
    internal async Task<TopStoriesResult> SelectArticles(List<ScrapedArticle> scrapedArticles)
    {
        List<NewsStory> newsStories = [];
        foreach (var article in scrapedArticles.OrderByDescending(s => s.LastUpdatedOn).Take(20))
        {
            if (string.IsNullOrWhiteSpace(article.Headline))
                continue;
            
            NewsStory newsStory = new()
            {
                Headline = article.Headline,
                Id = article.Id.ToString()
            };
            newsStories.Add(newsStory);
        }

        string file = "C:\\Users\\danny\\OneDrive\\Projects\\SnapshotNewsToday\\TestData\\top-stories-response_2025-11-18.json";
        var topStoryArticles = await provider.Select(newsStories, file);
        //var topStoryArticles = await provider.SelectArticles(sourceArticles);

        logger.Log(topStoryArticles.ToString());

        // Save Top Stories API call in top-stories-api-call


        // Save the Top Stories in top-stories table

        return topStoryArticles;
    }
}
