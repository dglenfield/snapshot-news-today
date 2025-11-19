using SnapshotJob.Common.Logging;
using SnapshotJob.Data.Models;
using SnapshotJob.Perplexity.Models.AnalyzeArticle;
using SnapshotJob.Perplexity.Models.TopStories.Response;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SnapshotJob.Perplexity;

public class ArticleProvider(IHttpClientFactory httpClientFactory, Logger logger)
{
    public async Task Analyze(ScrapedArticle article)
    {
        AnalyzeArticleResult analyzeArticleResult = new();

        string systemPromptFileName = "analyze-article-system-prompt.txt";
        string systemPromptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", systemPromptFileName);
        string systemContent = File.ReadAllText(systemPromptFilePath);

        string userPromptFileName = "analyze-article-user-prompt.txt";
        string userPromptFilePath = Path.Combine(AppContext.BaseDirectory, "Prompts", userPromptFileName);
        string userContent = $"{File.ReadAllText(userPromptFilePath)}{TrimInnerHtmlWhitespace(article.Content ?? string.Empty)}";

        var requestBody = new
        {
            model = "sonar",
            messages = new[]
            {
                new { role = "system", content = systemContent },
                new { role = "user", content = userContent }
            },
            web_search_options = new { search_context_size = "low" },
            max_tokens = 1800,
            temperature = 0.3,
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    schema = new
                    {
                        type = "array",
                        minItems = 0,
                        maxItems = 1,
                        items = new
                        {
                            type = "object",
                            required = new[] { "custom_headline", "summary", "key_points" },
                            properties = new
                            {
                                custom_headline = new { type = "string", description = "Custom headline" },
                                summary = new { type = "string", description = "Brief summary" },
                                key_points = new
                                {
                                    type = "array",
                                    minItems = 1,
                                    maxItems = 5,
                                    items = new { type = "string" },
                                    description = "Up to 5 key points"
                                }
                            },
                            additionalProperties = false
                        }
                    }
                }
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        //logger.Log(await jsonContent.ReadAsStringAsync());

        string file = "C:\\Users\\danny\\OneDrive\\Projects\\SnapshotNewsToday\\TestData\\analyze-article-response_2025-11-19.json";
        var responseString = await File.ReadAllTextAsync(file);

        //var response = await httpClientFactory.CreateClient("Perplexity").PostAsync("", jsonContent);

        //if (!response.IsSuccessStatusCode)
        //{
            //return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        //}

        //var responseString = await response.Content.ReadAsStringAsync();
        //logger.Log("\n" + responseString);
        //return Content(responseString, "application/json");

        var apiResponse = JsonSerializer.Deserialize<Response>(responseString);
        var contentJson = apiResponse?.Choices[0].Message.Content;
        
        // Remove opening and closing brackets
        contentJson = (contentJson?.Length > 2) ? contentJson.Substring(1, contentJson.Length - 2) : string.Empty;

        var analyzeArticleResponse = JsonSerializer.Deserialize<AnalyzeArticleContent>(contentJson);
        logger.Log("\n" + analyzeArticleResponse);

        analyzeArticleResult.PerplexityApiUsage = apiResponse.Usage;
    }

    private string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return Regex.Replace(html, @"\s+", " ").Trim();
    }
}
