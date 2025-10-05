using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Responses;

internal class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = default!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = default!;
}
