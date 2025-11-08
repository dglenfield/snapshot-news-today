using SnapshotJob.Common.Serialization;
using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Request;

public abstract class RequestBody
{
    /// <summary>
    /// OpenAI Compatible: The maximum number of completion tokens returned by the API.
    /// </summary>
    /// <remarks>Controls the length of the model's response. If the response would exceed this limit, it will 
    /// be truncated. Higher values allow for longer responses but may increase processing time and costs.</remarks>
    [JsonPropertyName("max_tokens")]
    [JsonPropertyOrderAttribute(1)]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// A list of messages comprising the conversation so far.
    /// </summary>
    [JsonPropertyOrderAttribute(5)]
    public required Message[] Messages { get; set; }

    /// <summary>
    /// The name of the model that will complete the prompt.
    /// </summary>
    [JsonPropertyOrderAttribute(0)]
    [JsonConverter(typeof(LowercaseJsonStringEnumConverter))]
    public Model Model { get; set; } = Model.Sonar;

    /// <summary>
    /// Placeholder for the formatting to use for the response. Derived classes must hide this property 
    /// using 'new' and provide a concrete implementation with the correct type and serialization attributes.
    /// </summary>
    /// <remarks>For example:
    /// <code>public new DesiredType ResponseFormat { get; init; }</code>
    /// Refer to <seealso cref="CurateArticles.Body"></seealso> for an actual implementation.
    /// </remarks>
    [JsonIgnore]
    [JsonPropertyName("response_format")]
    [JsonPropertyOrderAttribute(4)]
    public object? ResponseFormat2 
    { 
        get => null;
        init => throw new NotImplementedException("Implement in derived class using 'new' and the correct type.");
    }

    /// <summary>
    /// The amount of randomness in the response, valued between 0 and 2.
    /// </summary>
    /// <remarks>Lower values (e.g., 0.1) make the output more focused, deterministic, and less creative. 
    /// Higher values (e.g., 1.5) make the output more random and creative. Use lower values for 
    /// factual/information retrieval tasks and higher values for creative applications.</remarks>
    [JsonPropertyOrderAttribute(2)]
    public double? Temperature
    {
        get;
        set => field = (value > 0 && value < 2) ? value :
            throw new ArgumentOutOfRangeException(nameof(Temperature), "Value must be between 0 and 2 (exclusive)");
    }

    /// <summary>
    /// Perplexity-Specific: Configuration for using web search in model responses.
    /// </summary>
    [JsonPropertyName("web_search_options")]
    [JsonPropertyOrderAttribute(3)]
    public WebSearchOptions? WebSearchOptions { get; set; }
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
