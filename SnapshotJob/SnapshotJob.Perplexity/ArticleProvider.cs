using SnapshotJob.Common.Logging;
using SnapshotJob.Data.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SnapshotJob.Perplexity;

public class ArticleProvider(IHttpClientFactory httpClientFactory, Logger logger)
{
    public async Task Analyze(ScrapedArticle article)
    {
        string systemContent = @"
You are a news editor assistant.
Return the article summary as structured JSON with the following fields: 
custom_headline: Should be a catchy headline to grab user attention.
summary: A detailed and informative summary about the article.
key_points (up to 5): Each key point should be fully explained.";

        string userContent = $@"
Write a detailed summary for this article and a catchy headline to grab users' attention. 
Include up to 5 key points or interesting things about the news story. I encourage you to include quotes.
Here is the article content: {article.Content}
";

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

        string file = "C:\\Users\\danny\\OneDrive\\Projects\\SnapshotNewsToday\\TestData\\top-stories-response_2025-11-18.json";
        var responseString = await File.ReadAllTextAsync(file);

        //var response = await httpClientFactory.CreateClient("Perplexity").PostAsync("", jsonContent);

        //if (!response.IsSuccessStatusCode)
        //{
            //return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        //}

        //var responseString = await response.Content.ReadAsStringAsync();
        logger.Log(responseString);
        //return Content(responseString, "application/json");
    }

    private string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return Regex.Replace(html, @"\s+", " ").Trim();
    }
}
