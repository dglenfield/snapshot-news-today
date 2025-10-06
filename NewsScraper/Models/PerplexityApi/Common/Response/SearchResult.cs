using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Common.Response;

internal class SearchResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = default!;

    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    [JsonPropertyName("date")]
    public string Date { get; set; } = default!;

    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; } = default!;

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = default!;

    [JsonPropertyName("source")]
    public string Source { get; set; } = default!;
}
