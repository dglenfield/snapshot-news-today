using SnapshotJob.Common.Logging;
using SnapshotJob.Common.Serialization;
using SnapshotJob.Perplexity.Models.TopStories;
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

    public async Task<TopStoriesResult> Select(List<NewsStory> stories, string? testResponseFile = null)
    {
        // Log total news stories to select from
        _logger.Log($"Total stories to select from: {stories.Count}");

        if (stories.Count == 0)
            throw new Exception("No stories to select from.");

        List<NewsStory> headlines = [];
        foreach (var story in stories)
        {
            if (!string.IsNullOrWhiteSpace(story.Headline))
            {
                headlines.Add(new() { Headline = story.Headline, Id = story.Id });
            }
        }

        string systemPromptFileName = "top-stories-system-prompt.txt";
        string systemPromptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", systemPromptFileName);
        string systemContent = File.ReadAllText(systemPromptFilePath);

        string userPromptFileName = "top-stories-user-prompt.txt";
        string userPromptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", userPromptFileName);
        string userContent = $"{File.ReadAllText(userPromptFilePath)}{TrimInnerHtmlWhitespace(string.Join(Environment.NewLine, headlines))}";

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
        
        string responseString = string.Empty;
        if (!string.IsNullOrEmpty(testResponseFile))
        {
            // FOR TESTING: Read from test response file instead of calling API
            if (!string.IsNullOrEmpty(testResponseFile) && File.Exists(testResponseFile))
                responseString = await File.ReadAllTextAsync(testResponseFile);
        }
        else
        {
            // Call Perplexity API
            var response = await _httpClientFactory.CreateClient("Perplexity").PostAsync("", jsonContent);
            responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Perplexity API request failed with status code {response.StatusCode}: {responseString}");
        }

        // Log response JSON
        _logger.Log("\nResponse JSON:\n" + responseString, logAsRawMessage: true);
        
        // Deserialize the outer response
        var perplexityResponse = JsonSerializer.Deserialize<Response>(responseString);
        if (perplexityResponse?.Choices != null && perplexityResponse.Choices.Count > 0)
        {
            // The actual top stories are in the message content as JSON
            var contentJson = perplexityResponse.Choices[0].Message.Content;
            
            // Remove markdown code fences using regex
            contentJson = Regex.Replace(contentJson, @"^```(?:json)?\s*|\s*```$", "", RegexOptions.Multiline).Trim();

            // Deserialize the inner JSON
            var topStoriesResponse = JsonSerializer.Deserialize<TopStoriesContent>(contentJson);
            TopStoriesResult topNewsArticles = new()
            {
                TopStories = topStoriesResponse?.TopStories.Select(s => new NewsStory
                {
                    Headline = s.Headline,
                    Id = s.Id
                }).ToList() ?? [],
                SelectionCriteria = topStoriesResponse?.SelectionCriteriaText?.ToString() ?? string.Empty,
                ExcludedCategoriesList = topStoriesResponse?.ExcludedCategoriesList ?? [],
                Citations = perplexityResponse.Citations,
                SearchResults = perplexityResponse.SearchResults,
                PerplexityResponseId = perplexityResponse.Id,
                PerplexityResponseModel = perplexityResponse.Model,
                PerplexityApiUsage = perplexityResponse.Usage, 
                ResponseString = responseString
            };

            return topNewsArticles;
        }

        throw new Exception("Perplexity API response contained no choices.");
    }

    private string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return Regex.Replace(html, @"\s+", " ").Trim();
    }
}
