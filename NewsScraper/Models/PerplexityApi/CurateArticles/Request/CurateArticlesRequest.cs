using NewsScraper.Models.PerplexityApi.Common.Request;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.CurateArticles.Request;

/// <summary>
/// The request body for the Sonar Chat Completions API.
/// </summary>
internal class CurateArticlesRequest : RequestBody
{
    /// <summary>
    /// The formatting to use for the response.
    /// </summary>
    [JsonPropertyName("response_format")]
    [JsonPropertyOrderAttribute(4)]
    public new JsonResponseFormat ResponseFormat { get; init; }

    /// <summary>
    /// Initializes a new instance of the CurateArticlesRequestBody class with default values.
    /// </summary>
    internal CurateArticlesRequest() : base()
    {
        MaxTokens = 2000;
        ResponseFormat = new JsonResponseFormat() { Schema = new JsonSchema() };
        WebSearchOptions = new() { SearchContextSize = SearchContextSize.Low };
    }
}
