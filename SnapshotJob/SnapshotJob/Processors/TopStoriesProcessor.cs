using SnapshotJob.Common.Logging;
using SnapshotJob.Data.Models;
using SnapshotJob.Data.Repositories;
using SnapshotJob.Perplexity;
using SnapshotJob.Perplexity.Models.TopStories;

namespace SnapshotJob.Processors;

internal class TopStoriesProcessor(TopStoriesProvider provider, Logger logger, TopStoryApiCallRepository topStoryApiCallRepository)
{
    internal async Task<TopStoriesResult> SelectStories(List<ScrapedArticle> scrapedArticles, long snapshotId)
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
        var topStoriesResult = await provider.Select(newsStories, file);
        //var topStoryArticles = await provider.SelectArticles(sourceArticles);

        logger.Log(topStoriesResult.ToString());

        // Save Top Stories API call in top-stories-api-call
        TopStoryApiCall apiCall = new() 
        {
            CompletionTokens = topStoriesResult.PerplexityApiUsage.CompletionTokens,
            PromptTokens = topStoriesResult.PerplexityApiUsage.PromptTokens,
            TotalTokens = topStoriesResult.PerplexityApiUsage.TotalTokens,
            InputTokensCost = topStoriesResult.PerplexityApiUsage.Cost.InputTokensCost,
            OutputTokensCost = topStoriesResult.PerplexityApiUsage.Cost.OutputTokensCost,
            RequestCost = topStoriesResult.PerplexityApiUsage.Cost.RequestCost,
            TotalCost = topStoriesResult.PerplexityApiUsage.Cost.TotalCost,
            ResponseString = topStoriesResult.ResponseString
        };

        logger.Log(apiCall.ResponseString.ToString());

        await topStoryApiCallRepository.CreateAsync(apiCall, snapshotId);

        // Save the Top Stories in top-stories table

        return topStoriesResult;
    }
}
