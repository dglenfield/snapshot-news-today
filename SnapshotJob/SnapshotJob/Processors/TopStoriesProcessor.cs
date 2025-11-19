using SnapshotJob.Common.Logging;
using SnapshotJob.Data.Models;
using SnapshotJob.Data.Repositories;
using SnapshotJob.Perplexity;
using SnapshotJob.Perplexity.Models.TopStories;

namespace SnapshotJob.Processors;

internal class TopStoriesProcessor(TopStoriesProvider provider, Logger logger, 
    TopStoryApiCallRepository topStoryApiCallRepository, TopStoryRepository topStoryRepository)
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

        // Save API call in database
        TopStoryApiCall apiCall = new() 
        {
            CompletionTokens = topStoriesResult.PerplexityApiUsage.CompletionTokens,
            PromptTokens = topStoriesResult.PerplexityApiUsage.PromptTokens,
            TotalTokens = topStoriesResult.PerplexityApiUsage.TotalTokens,
            InputTokensCost = topStoriesResult.PerplexityApiUsage.Cost.InputTokensCost,
            OutputTokensCost = topStoriesResult.PerplexityApiUsage.Cost.OutputTokensCost,
            RequestCost = topStoriesResult.PerplexityApiUsage.Cost.RequestCost,
            TotalCost = topStoriesResult.PerplexityApiUsage.Cost.TotalCost,
            RequestBody = topStoriesResult.RequestBody,
            ResponseString = topStoriesResult.ResponseString,
            Exception = topStoriesResult.Exception
        };
        await topStoryApiCallRepository.CreateAsync(apiCall, snapshotId);

        // Save Top Stories in database
        foreach (var topStory in topStoriesResult.TopStories)
        {
            if (long.TryParse(topStory.Id, out long scrapedArticleId) 
                && !await topStoryRepository.ExistsAsync(scrapedArticleId))
            {
                await topStoryRepository.CreateAsync(new()
                {
                    Headline = topStory.Headline,
                    ScrapedArticleId = scrapedArticleId
                });
            }
        }

        return topStoriesResult;
    }
}
