using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.CurateArticles.Response;

internal class Story
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    [JsonPropertyName("headline")]
    public string Headline { get; set; } = default!;

    [JsonPropertyName("category")]
    public string Category { get; set; } = default!;

    [JsonPropertyName("highlights")]
    public string Highlights { get; set; } = default!;

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = default!;
}
