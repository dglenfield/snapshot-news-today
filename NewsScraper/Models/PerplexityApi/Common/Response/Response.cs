using NewsScraper.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Common.Response;

/// <summary>
/// Represents the response returned from a Perplexity API operation, including metadata, usage statistics, and result data.
/// </summary>
/// <remarks>The Response class encapsulates all relevant information returned by the API. This type is used to 
/// deserialize and process API responses</remarks>
internal class Response
{
    /// <summary>
    /// Gets the unique identifier for this instance.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    /// <summary>
    /// Gets the name of the model used for generation.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; init; } = default!;

    /// <summary>
    /// Gets the Unix timestamp indicating when the resource was created.
    /// </summary>
    [JsonPropertyName("created")]
    public long Created { get; init; }

    /// <summary>
    /// Gets the usage statistics for the current operation.
    /// </summary>
    [JsonPropertyName("usage")]
    public Usage Usage { get; init; } = default!;

    /// <summary>
    /// Gets the list of citation references associated with this item.
    /// </summary>
    [JsonPropertyName("citations")]
    public List<string> Citations { get; init; } = default!;

    /// <summary>
    /// Gets the collection of search results returned by the query.
    /// </summary>
    [JsonPropertyName("search_results")]
    public List<SearchResult> SearchResults { get; init; } = default!;

    /// <summary>
    /// Gets the type of object represented by this instance as a string identifier.
    /// </summary>
    /// <remarks>The value typically indicates the resource type returned by the API, such as
    /// "chat.completion". This property is useful for distinguishing between different kinds of API responses when
    /// processing results.</remarks>
    [JsonPropertyName("object")]
    public string Object { get; init; } = default!;

    /// <summary>
    /// Gets the collection of choices returned by the API response.
    /// </summary>
    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; init; } = default!;

    // JSON serialization methods
    public string ToJson() => JsonSerializer.Serialize(this);
    public string ToJson(JsonSerializerOptions options) => JsonSerializer.Serialize(this, options);
    public string ToJson(JsonSerializerOptions options, CustomJsonSerializerOptions customOptions) =>
        JsonSerializer.Serialize(this, JsonConfig.Customize(options, customOptions));

    // Override ToString() to return indented JSON with null values ignored
    public override string ToString() => ToJson(JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
