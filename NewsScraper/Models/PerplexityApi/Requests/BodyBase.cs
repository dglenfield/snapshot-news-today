using NewsScraper.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Requests;

internal abstract class BodyBase
{
    /// <summary>
    /// OpenAI Compatible: The maximum number of completion tokens returned by the API.
    /// </summary>
    /// <remarks>Controls the length of the model's response. If the response would exceed this limit, it will 
    /// be truncated. Higher values allow for longer responses but may increase processing time and costs.</remarks>
    [JsonPropertyName("max_tokens")]
    [JsonPropertyOrderAttribute(3)]
    public int? Max_Tokens { get; set; }

    /// <summary>
    /// A list of messages comprising the conversation so far.
    /// </summary>
    [JsonPropertyOrderAttribute(1)]
    public required Message[] Messages { get; set; }

    /// <summary>
    /// The name of the model that will complete the prompt.
    /// </summary>
    [JsonPropertyOrderAttribute(0)]
    [JsonConverter(typeof(LowercaseJsonStringEnumConverter))]
    public Model Model { get; set; } = Model.Sonar;

    /// <summary>
    /// Perplexity-Specific: Configuration for using web search in model responses.
    /// </summary>
    [JsonPropertyName("web_search_options")]
    [JsonPropertyOrderAttribute(2)]
    public WebSearchOptions? Web_Search_Options { get; set; }

    /// <summary>
    /// The amount of randomness in the response, valued between 0 and 2.
    /// </summary>
    /// <remarks>Lower values (e.g., 0.1) make the output more focused, deterministic, and less creative. 
    /// Higher values (e.g., 1.5) make the output more random and creative. Use lower values for 
    /// factual/information retrieval tasks and higher values for creative applications.</remarks>
    [JsonPropertyOrderAttribute(4)]
    public double? Temperature
    {
        get;
        set => field = (value > 0 && value < 2) ? value :
                throw new ArgumentOutOfRangeException(nameof(Temperature), "Value must be between 0 and 2 (exclusive)");
    }

    public string ToJson() => JsonSerializer.Serialize(this);
    public string ToJson(JsonSerializerOptions options) => JsonSerializer.Serialize(this, options);
    public string ToJson(JsonSerializerOptions options, CustomJsonSerializerOptions customOptions) =>
        JsonSerializer.Serialize(this, JsonConfig.Customize(options, customOptions));

    public override string ToString() => ToJson(JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}

/// <summary>
/// Available Sonar models: sonar (lightweight search), sonar pro (advanced search), 
/// sonar deep research (exhaustive research), sonar reasoning (fast reasoning), or 
/// sonar reasoning pro (premier reasoning).
/// </summary>
public enum Model
{
    [JsonPropertyName("sonar")]
    Sonar,
    [JsonPropertyName("sonar-pro")]
    SonarPro,
    [JsonPropertyName("sonar-deep-research")]
    SonarDeepResearch,
    [JsonPropertyName("sonar-reasoning")]
    SonarReasoning,
    [JsonPropertyName("sonar-reasoning-pro")]
    SonarReasoningPro
}
