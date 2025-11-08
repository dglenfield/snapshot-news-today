using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Response;

public class SearchResult
{
    /// <summary>
    /// Gets the title of the search result.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = default!;

    /// <summary>
    /// Gets the URL for this search result.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; init; } = default!;

    /// <summary>
    /// Gets the date associated with the search result, formatted as a string.
    /// </summary>
    [JsonPropertyName("date")]
    public string Date { get; init; } = default!;

    /// <summary>
    /// Gets the date and time when the entity was last updated.
    /// </summary>
    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; init; } = default!;

    /// <summary>
    /// Gets the text snippet associated with the search result.
    /// </summary>
    [JsonPropertyName("snippet")]
    public string Snippet { get; init; } = default!;

    /// <summary>
    /// Gets the source for the search result.
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; init; } = default!;
}
