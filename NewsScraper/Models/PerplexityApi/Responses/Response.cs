using NewsScraper.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Responses;

internal class Response
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("model")]
    public string Model { get; set; } = default!;

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("usage")]
    public Usage Usage { get; set; } = default!;

    [JsonPropertyName("citations")]
    public List<string> Citations { get; set; } = default!;

    [JsonPropertyName("search_results")]
    public List<SearchResult> SearchResults { get; set; } = default!;

    [JsonPropertyName("object")]
    public string Object { get; set; } = default!;

    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; } = default!;

    public string ToJson() => JsonSerializer.Serialize(this);
    public string ToJson(JsonSerializerOptions options) => JsonSerializer.Serialize(this, options);
    public string ToJson(JsonSerializerOptions options, CustomJsonSerializerOptions customOptions) =>
        JsonSerializer.Serialize(this, JsonConfig.Customize(options, customOptions));

    public override string ToString() => ToJson(JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
