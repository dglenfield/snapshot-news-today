using NewsScraper.Logging;
using NewsScraper.Models;
using NewsScraper.Models.PerplexityApi.Requests;
using NewsScraper.Models.PerplexityApi.Responses;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CurateArticles = NewsScraper.Models.PerplexityApi.Requests.CurateArticles;

namespace NewsScraper.Providers;

internal class PerplexityApiProvider(IHttpClientFactory httpClientFactory, string test = "")
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    internal async Task CurateArticles(List<NewsArticle> articles)
    {
        List<Uri> distinctArticleUris = [.. articles.Select(a => a.SourceUri).Distinct()];
        Logger.Log($"Total articles to curate from: {articles.Count}");

        string systemPromptFileName = "curate-articles-system-prompt.txt";
        string systemPromptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", systemPromptFileName);
        string systemContent = File.ReadAllText(systemPromptFilePath);

        string userPromptFileName = "curate-articles-user-prompt.txt";
        string userPromptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", userPromptFileName);
        string userContent = $"{File.ReadAllText(userPromptFilePath)}\n{string.Join(Environment.NewLine, distinctArticleUris.Select(u => u.AbsoluteUri))}";

        CurateArticles.Body requestBody = new() { Messages = [new(Role.System, systemContent), new(Role.User, userContent)] };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web), Encoding.UTF8, "application/json");
        Logger.Log(jsonContent.ReadAsStringAsync().GetAwaiter().GetResult(), logAsRawMessage: true);

        // FOR TESTING: Read from test response file instead of calling API
        string testResponseString = string.Empty;
        bool useTestResponseFile = Configuration.TestSettings.PerplexityApiProvider.CurateArticles.UseTestResponseFile;
        string testResponseFile = Configuration.TestSettings.PerplexityApiProvider.CurateArticles.TestResponseFile;
        if (useTestResponseFile && !string.IsNullOrEmpty(testResponseFile) && File.Exists(testResponseFile))
            testResponseString = await File.ReadAllTextAsync(testResponseFile);
        
        return;

        // Call Perplexity API
        var response = await _httpClientFactory.CreateClient("Perplexity").PostAsync("", jsonContent);
        var responseString = await response.Content.ReadAsStringAsync();

        Logger.Log(responseString, logAsRawMessage: true);
        Logger.Log($"HTTP Status Code: {response.StatusCode}");
        if (!response.IsSuccessStatusCode)
        {
            return;
            //return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        // Deserialize the outer response
        var perplexityResponse = JsonSerializer.Deserialize<PerplexityResponse>(responseString);

        if (perplexityResponse?.Choices != null && perplexityResponse.Choices.Count > 0)
        {
            // The actual curated articles are in the message content as JSON
            var contentJson = perplexityResponse.Choices[0].Message.Content;

            // Remove markdown code fences using regex
            contentJson = Regex.Replace(contentJson, @"^```(?:json)?\s*|\s*```$", "", RegexOptions.Multiline).Trim();

            // Remove markdown code fences if present
            //contentJson = contentJson.Trim();
            //if (contentJson.StartsWith("```json"))
            //{
            //    contentJson = contentJson.Substring(7); // Remove ```json
            //}
            //else if (contentJson.StartsWith("```"))
            //{
            //    contentJson = contentJson.Substring(3); // Remove ```
            //}

            //if (contentJson.EndsWith("```"))
            //{
            //    contentJson = contentJson.Substring(0, contentJson.Length - 3); // Remove trailing ```
            //}

            //contentJson = contentJson.Trim(); // Clean up any extra whitespace

            Logger.Log("Cleaned JSON content:");
            Logger.Log(contentJson, logAsRawMessage: true);

            // Deserialize the inner JSON
            var curatedArticles = JsonSerializer.Deserialize<CuratedArticlesResponse>(contentJson);

            if (curatedArticles?.TopStories != null)
            {
                foreach (var story in curatedArticles.TopStories)
                {
                    Logger.Log($"Headline: {story.Headline}");
                    Logger.Log($"URL: {story.Url}");
                    Logger.Log($"Category: {story.Category}");
                    Logger.Log($"Highlights: {story.Highlights}");
                    Logger.Log($"Rationale: {story.Rationale}");
                    Logger.Log("---");
                }

                // Handle selection criteria (string or object)
                Logger.Log($"Selection Criteria: {curatedArticles.SelectionCriteriaText}");

                // Handle excluded categories (array or object)
                Logger.Log($"Excluded Categories: {string.Join(", ", curatedArticles.ExcludedCategoriesList)}");
            }
        }
    }
}
