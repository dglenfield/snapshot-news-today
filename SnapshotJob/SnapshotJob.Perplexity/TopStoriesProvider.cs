using SnapshotJob.Common.Logging;
using SnapshotJob.Common.Serialization;
using SnapshotJob.Perplexity.Models;
using SnapshotJob.Perplexity.Models.TopStories.Request;
using SnapshotJob.Perplexity.Models.TopStories.Response;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SnapshotJob.Perplexity;

public class TopStoriesProvider(IHttpClientFactory httpClientFactory, Logger logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly Logger _logger = logger;

    public async Task<TopStoryArticles> SelectArticles(List<SourceNewsArticle> articles)
    {
        // Get only the URIs of articles published within the past 2 days
        DateTime twoDaysAgo = DateTime.UtcNow.AddDays(-2);
        //if (Configuration.TestSettings.PerplexityApiProvider.CurateArticles.UseTestResponseFile)
        //    twoDaysAgo = DateTime.MinValue; // Include all articles if using test data
        List<Uri> recentArticleUris = [ .. articles
                .Where(a => a.LastUpdatedOn.HasValue && a.LastUpdatedOn.Value >= twoDaysAgo)
                .Select(a => a.SourceUri)
                .Distinct()
        ];

        // Log total articles to curate from
        _logger.Log($"Total articles to curate from: {recentArticleUris.Count}");

        if (recentArticleUris.Count == 0)
            throw new Exception("No recent articles to curate from.");

        string systemPromptFileName = "curate-articles-system-prompt.txt";
        string systemPromptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", systemPromptFileName);
        string systemContent = File.ReadAllText(systemPromptFilePath);

        string userPromptFileName = "curate-articles-user-prompt.txt";
        string userPromptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", userPromptFileName);
        string userContent = $"{File.ReadAllText(userPromptFilePath)}\n{string.Join(Environment.NewLine, recentArticleUris.Select(u => u.AbsoluteUri))}";

        // Construct request body
        TopStoriesRequestBody requestBody = new()
        {
            Messages = [new(Role.System, systemContent), new(Role.User, userContent)]
        };

        // Serialize request body to JSON
        var jsonContent = new StringContent(JsonConfig.ToJson(requestBody, JsonSerializerOptions.Web, 
            CustomJsonSerializerOptions.IgnoreNull), Encoding.UTF8, "application/json");

        // Log request JSON
        _logger.Log("\nRequest JSON:\n" + jsonContent.ReadAsStringAsync().GetAwaiter().GetResult(), logAsRawMessage: true);
        
        // FOR TESTING: Read from test response file instead of calling API
        string testResponseString = string.Empty;
        //bool useTestResponseFile = Configuration.TestSettings.PerplexityApiProvider.CurateArticles.UseTestResponseFile;
        //string testResponseFile = Configuration.TestSettings.PerplexityApiProvider.CurateArticles.TestResponseFile;
        //if (useTestResponseFile && !string.IsNullOrEmpty(testResponseFile) && File.Exists(testResponseFile))
        //    testResponseString = await File.ReadAllTextAsync(testResponseFile);

        string responseString = string.Empty;
        //if (useTestResponseFile) 
        //    responseString = testResponseString;
        //else
        //{
            // Call Perplexity API
            var response = await _httpClientFactory.CreateClient("Perplexity").PostAsync("", jsonContent);
            responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Perplexity API request failed with status code {response.StatusCode}: {responseString}");
        //}

        // Log response JSON (if not using test data)
        //if (!Configuration.TestSettings.PerplexityApiProvider.CurateArticles.UseTestResponseFile)
        //    _logger.Log("\nResponse JSON:\n" + responseString, logAsRawMessage: true);
        
        // Deserialize the outer response
        var perplexityResponse = JsonSerializer.Deserialize<Response>(responseString);
        if (perplexityResponse?.Choices != null && perplexityResponse.Choices.Count > 0)
        {
            // The actual curated articles are in the message content as JSON
            var contentJson = perplexityResponse.Choices[0].Message.Content;
            
            // Remove markdown code fences using regex
            contentJson = Regex.Replace(contentJson, @"^```(?:json)?\s*|\s*```$", "", RegexOptions.Multiline).Trim();

            // Deserialize the inner JSON
            var curatedArticlesResponse = JsonSerializer.Deserialize<TopStoriesContent>(contentJson);
            TopStoryArticles curatedNewsArticles = new()
            {
                Articles = curatedArticlesResponse?.TopStories.Select(s => new TopStoryArticle
                {
                    SourceUri = new Uri(s.Url),
                    CuratedHeadline = s.Headline,
                    CuratedCategory = s.Category,
                    Highlights = s.Highlights,
                    Rationale = s.Rationale
                }).ToList() ?? [],
                SelectionCriteria = curatedArticlesResponse?.SelectionCriteriaText?.ToString() ?? string.Empty,
                ExcludedCategoriesList = curatedArticlesResponse?.ExcludedCategoriesList ?? [],
                Citations = perplexityResponse.Citations,
                SearchResults = perplexityResponse.SearchResults,
                PerplexityResponseId = perplexityResponse.Id,
                PerplexityResponseModel = perplexityResponse.Model,
                PerplexityApiUsage = perplexityResponse.Usage
            };
            
            // Merge source article details into curated articles
            curatedNewsArticles.Articles
                .Where(ca => articles.Any(a => a.SourceUri == ca.SourceUri))
                .ToList()
                .ForEach(ca =>
                {
                    var sourceArticle = articles.First(a => a.SourceUri == ca.SourceUri);
                    ca.Headline = sourceArticle.Headline;
                    ca.LastUpdatedOn = sourceArticle.LastUpdatedOn;
                    //ca.SourceName = sourceArticle.SourceName;
                    ca.Category = sourceArticle.Category;
                });

            return curatedNewsArticles;
        }

        throw new Exception("Perplexity API response contained no choices.");
    }
}
