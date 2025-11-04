using System.Text.Json.Serialization;

namespace Common.Models.PerplexityApi.CurateArticles.Response;

/// <summary>
/// Represents a news story, including its headline, category, highlights, rationale, and article URL.
/// </summary>
public class Story
{
    /// <summary>
    /// Gets the URL for the news article.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; init; } = default!;

    /// <summary>
    /// Gets the headline for the news article.
    /// </summary>
    [JsonPropertyName("headline")]
    public string Headline { get; init; } = default!;

    /// <summary>
    /// Gets the category associated with the news article.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; init; } = default!;

    /// <summary>
    /// Gets the highlights of the news story.
    /// </summary>
    [JsonPropertyName("highlights")]
    public string Highlights { get; init; } = default!;

    /// <summary>
    /// Gets the rationale for including this news story.
    /// </summary>
    [JsonPropertyName("rationale")]
    public string Rationale { get; init; } = default!;
}
