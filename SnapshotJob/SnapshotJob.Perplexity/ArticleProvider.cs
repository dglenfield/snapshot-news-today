using Microsoft.Extensions.Options;
using SnapshotJob.Data.Models;
using SnapshotJob.Perplexity.Configuration.Options;
using SnapshotJob.Perplexity.Models.AnalyzeArticle;
using SnapshotJob.Perplexity.Models.ApiResponse;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SnapshotJob.Perplexity;

public class ArticleProvider(IHttpClientFactory httpClientFactory, IOptions<PerplexityOptions> options)
{
    public async Task<AnalyzeArticleResult> Analyze(ScrapedArticle article)
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
        string responseString = string.Empty;
        try
        {
            if (options.Value.UseArticleTestFile)
                responseString = await File.ReadAllTextAsync(options.Value.ArticleTestFile);
            else
            {
                var response = await httpClientFactory.CreateClient("Perplexity").PostAsync("", jsonContent);
                responseString = await response.Content.ReadAsStringAsync();
            }

            var apiResponse = JsonSerializer.Deserialize<Response>(responseString);
            if (apiResponse is not null)
            {
                var contentJson = apiResponse?.Choices[0].Message.Content;

                // Remove opening and closing brackets
                contentJson = (contentJson?.Length > 2) ? contentJson.Substring(1, contentJson.Length - 2) : string.Empty;

                analyzeArticleResult.Content = JsonSerializer.Deserialize<AnalyzeArticleContent>(contentJson);
                analyzeArticleResult.Citations = apiResponse?.Citations;
                analyzeArticleResult.SearchResults = apiResponse?.SearchResults;
                analyzeArticleResult.PerplexityResponseId = apiResponse?.Id;
                analyzeArticleResult.PerplexityResponseModel = apiResponse?.Model;
                analyzeArticleResult.PerplexityApiUsage = apiResponse?.Usage;
            }
        }
        catch (Exception ex)
        {
            analyzeArticleResult.Exception = ex;
        }
        finally
        {
            if (jsonContent is not null)
                analyzeArticleResult.RequestBody = jsonContent.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(responseString))
                analyzeArticleResult.ResponseString = responseString;
        }

        return analyzeArticleResult;
    }

    private string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return Regex.Replace(html, @"\s+", " ").Trim();
    }
}
