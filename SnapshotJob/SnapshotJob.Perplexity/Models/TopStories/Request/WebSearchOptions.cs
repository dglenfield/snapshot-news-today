using SnapshotJob.Common.Serialization;
using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Request;

/// <summary>
/// Perplexity-Specific: Configuration for using web search in model responses.
/// </summary>
public class WebSearchOptions
{
    /// <summary>
    /// The search context to use for web search in model responses. The default is low.
    /// </summary>
    [JsonPropertyName("search_context_size")]
    [JsonConverter(typeof(LowercaseJsonStringEnumConverter))]
    public SearchContextSize? SearchContextSize { get; set; }

    /// <summary>
    /// When enabled, improves the relevance of image search results to the user query. 
    /// Enhanced images will be streamed in later chunks of the response.
    /// </summary>
    [JsonPropertyName("image_search_relevance_enhanced")]
    public bool? ImageSearchRelevanceEnhanced { get; set; }

    /// <summary>
    /// The approximate user location to refine search results based on geography.
    /// </summary>
    [JsonPropertyName("user_location")]
    public string? UserLocation { get; set; }
}

/// <summary>
/// Determines how much search context is retrieved for the model.
/// </summary>
/// <remarks>Options are: low (minimizes context for cost savings but less comprehensive answers), 
/// medium (balanced approach suitable for most queries), 
/// and high (maximizes context for comprehensive answers but at higher cost).</remarks>
public enum SearchContextSize
{
    Low,
    Medium,
    High
}
