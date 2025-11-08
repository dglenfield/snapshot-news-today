using SnapshotJob.Common.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Request;

/// <summary>
/// The request body for the Sonar Chat Completions API.
/// </summary>
public class TopStoriesRequestBody : RequestBody
{
    /// <summary>
    /// The formatting to use for the response.
    /// </summary>
    [JsonPropertyName("response_format")]
    [JsonPropertyOrderAttribute(4)]
    public JsonResponseFormat ResponseFormat { get; init; } = new();

    /// <summary>
    /// Initializes a new instance of the CurateArticlesRequestBody class with default values.
    /// </summary>
    public TopStoriesRequestBody() : base()
    {
        MaxTokens = 2000;
        WebSearchOptions = new() { SearchContextSize = SearchContextSize.Low };
    }

    /// <summary>
    /// Returns a JSON-formatted string representation of the current object.
    /// </summary>
    /// <remarks>The returned JSON string uses default serialization options, including indentation for
    /// readability and omission of properties with null values. This can be useful for logging, debugging, or
    /// persisting the object's state in a human-readable format.</remarks>
    /// <returns>A string containing the JSON representation of the object, formatted with indentation and excluding properties
    /// with null values.</returns>
    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
