using Microsoft.Extensions.Options;
using SnapshotJob.Data.Models;
using SnapshotJob.Data.Repositories;
using SnapshotJob.Perplexity;
using SnapshotJob.Perplexity.Configuration.Options;
using SnapshotJob.Perplexity.Models.TopStories;

namespace SnapshotJob.Processors;

internal class TopStoriesProcessor(TopStoriesProvider provider, 
    PerplexityApiCallRepository topStoryApiCallRepository, TopStoryRepository topStoryRepository, 
    IOptions<PerplexityOptions> options)
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

        Console.WriteLine(options.Value.TopStoriesTestFile);
        TopStoriesResult topStoriesResult;
        if (options.Value.UseTopStoriesTestFile)
            topStoriesResult = await provider.Select(newsStories, options.Value.TopStoriesTestFile);
        else
            topStoriesResult = await provider.Select(newsStories);
        
        // Save API call to the database
        PerplexityApiCall apiCall = new() 
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

        // Save Top Stories to the database
        foreach (NewsStory topStory in topStoriesResult.TopStories)
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
