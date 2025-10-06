using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Common.Response;

internal class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("search_context_size")]
    public string SearchContextSize { get; set; } = default!;

    [JsonPropertyName("cost")]
    public Cost Cost { get; set; } = default!;
}
