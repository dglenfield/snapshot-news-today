using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Responses;

internal class Cost
{
    [JsonPropertyName("input_tokens_cost")]
    public double InputTokensCost { get; set; }

    [JsonPropertyName("output_tokens_cost")]
    public double OutputTokensCost { get; set; }

    [JsonPropertyName("request_cost")]
    public double RequestCost { get; set; }

    [JsonPropertyName("total_cost")]
    public double TotalCost { get; set; }
}
