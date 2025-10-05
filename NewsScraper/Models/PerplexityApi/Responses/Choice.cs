using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Responses;

internal class Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = default!;

    [JsonPropertyName("message")]
    public Message Message { get; set; } = default!;

    [JsonPropertyName("delta")]
    public Delta Delta { get; set; } = default!;
}
