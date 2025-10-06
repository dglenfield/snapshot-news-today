using NewsScraper.Logging;
using NewsScraper.Models;
using NewsScraper.Models.PerplexityApi.Common.Request;
using NewsScraper.Models.PerplexityApi.Common.Response;
using NewsScraper.Models.PerplexityApi.CurateArticles.Request;
using NewsScraper.Models.PerplexityApi.CurateArticles.Response;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NewsScraper.Providers;

internal class PerplexityApiProvider(IHttpClientFactory httpClientFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    internal async Task<CuratedNewsArticles> CurateArticles(List<NewsArticle> articles)
    {
        List<Uri> distinctArticleUris = [.. articles.Select(a => a.SourceUri).Distinct()];
        Logger.Log($"Total articles to curate from: {articles.Count}");

        string systemPromptFileName = "curate-articles-system-prompt.txt";
        string systemPromptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", systemPromptFileName);
        string systemContent = File.ReadAllText(systemPromptFilePath);

        string userPromptFileName = "curate-articles-user-prompt.txt";
        string userPromptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", userPromptFileName);
        string userContent = $"{File.ReadAllText(userPromptFilePath)}\n{string.Join(Environment.NewLine, distinctArticleUris.Select(u => u.AbsoluteUri))}";

        CurateArticlesRequest requestBody = new() 
        { 
            Messages = [new(Role.System, systemContent), new(Role.User, userContent)] 
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web), Encoding.UTF8, "application/json");
        Logger.Log("\nRequest JSON:\n" + jsonContent.ReadAsStringAsync().GetAwaiter().GetResult(), logAsRawMessage: true);
        //return null;
        // FOR TESTING: Read from test response file instead of calling API
        string testResponseString = string.Empty;
        bool useTestResponseFile = Configuration.TestSettings.PerplexityApiProvider.CurateArticles.UseTestResponseFile;
        string testResponseFile = Configuration.TestSettings.PerplexityApiProvider.CurateArticles.TestResponseFile;
        if (useTestResponseFile && !string.IsNullOrEmpty(testResponseFile) && File.Exists(testResponseFile))
            testResponseString = await File.ReadAllTextAsync(testResponseFile);

        string responseString = string.Empty;
        if (useTestResponseFile) 
            responseString = testResponseString;
        else
        {
            // Call Perplexity API
            var response = await _httpClientFactory.CreateClient("Perplexity").PostAsync("", jsonContent);
            responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Perplexity API request failed with status code {response.StatusCode}: {responseString}");
        }        
        Logger.Log("\nResponse JSON:\n" + responseString, logAsRawMessage: true);
        
        // Deserialize the outer response
        var perplexityResponse = JsonSerializer.Deserialize<Response>(responseString);
        if (perplexityResponse?.Choices != null && perplexityResponse.Choices.Count > 0)
        {
            // The actual curated articles are in the message content as JSON
            var contentJson = perplexityResponse.Choices[0].Message.Content;

            // Remove markdown code fences using regex
            contentJson = Regex.Replace(contentJson, @"^```(?:json)?\s*|\s*```$", "", RegexOptions.Multiline).Trim();

            // Deserialize the inner JSON
            var curatedArticlesResponse = JsonSerializer.Deserialize<CuratedArticlesResponse>(contentJson);
            CuratedNewsArticles curatedNewsArticles = new()
            {
                Articles = curatedArticlesResponse?.TopStories.Select(s => new CuratedNewsArticle
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
                    ca.SourceHeadline = sourceArticle.SourceHeadline;
                    ca.SourcePublishDate = sourceArticle.SourcePublishDate;
                    ca.SourceName = sourceArticle.SourceName;
                    ca.SourceCategory = sourceArticle.SourceCategory;
                });

            Logger.Log($"\nCurated news articles:\n{curatedNewsArticles}");

            return curatedNewsArticles;
        }

        throw new Exception("Perplexity API response contained no choices.");
    }
}
