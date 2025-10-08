using NewsAnalyzer.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsAnalyzer.Models.PerplexityApi.Common.Response;

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

    /// <summary>
    /// Returns a JSON-formatted string that represents the current object.
    /// </summary>
    /// <remarks>The returned JSON string uses default serialization options and omits properties with null
    /// values for readability. This method is useful for logging, debugging, or exporting the object's state.</remarks>
    /// <returns>A string containing the JSON representation of the object, formatted with indentation and excluding properties
    /// with null values.</returns>
    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
