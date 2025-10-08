using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Common.Response;

/// <summary>
/// Represents token usage and related cost information for a request and its response.
/// </summary>
/// <remarks>The Usage class provides details about the number of tokens consumed during both the prompt and
/// completion phases of a request, as well as the total tokens processed and associated cost data. This information can
/// be used to monitor resource consumption and estimate billing for API interactions.</remarks>
internal class Usage
{
    /// <summary>
    /// Gets the number of tokens consumed by the prompt portion of the request.
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; init; }

    /// <summary>
    /// Gets the number of tokens consumed in generating the completion output.
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; init ; }

    /// <summary>
    /// Gets the total number of tokens processed in the request and response.
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; init; }

    /// <summary>
    /// Gets the size of the search context as a string value.
    /// </summary>
    [JsonPropertyName("search_context_size")]
    public string SearchContextSize { get; init; } = default!;

    /// <summary>
    /// Gets the cost information associated with the current item.
    /// </summary>
    [JsonPropertyName("cost")]
    public Cost Cost { get; init; } = default!;
}
