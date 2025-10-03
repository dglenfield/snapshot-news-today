namespace NewsScraper.Models.PerplexityApi.Requests.CurateArticles;

/// <summary>
/// The request body for the Sonar Chat Completions API.
/// </summary>
internal class Body : BodyBase
{
    /// <summary>
    /// OpenAI Compatible: The maximum number of completion tokens returned by the API.
    /// </summary>
    /// <remarks>Controls the length of the model's response. If the response would exceed this limit, it will 
    /// be truncated. Higher values allow for longer responses but may increase processing time and costs.</remarks>
    //[JsonPropertyName("max_tokens")]
    //[JsonPropertyOrderAttribute(3)]
    //public int? Max_Tokens { get; set; }

    /// <summary>
    /// A list of messages comprising the conversation so far.
    /// </summary>
    //[JsonPropertyOrderAttribute(1)]
    //public required Message[] Messages { get; set; }

    /// <summary>
    /// The name of the model that will complete the prompt.
    /// </summary>
    //[JsonPropertyOrderAttribute(0)]
    //[JsonConverter(typeof(LowercaseJsonStringEnumConverter))]
    //public Model Model { get; set; } = Model.Sonar;

    /// <summary>
    /// The formatting to use for the response.
    /// </summary>
    //[JsonPropertyOrderAttribute(5)]
    public ResponseFormat Response_Format { get; init; }

    /// <summary>
    /// The amount of randomness in the response, valued between 0 and 2.
    /// </summary>
    /// <remarks>Lower values (e.g., 0.1) make the output more focused, deterministic, and less creative. 
    /// Higher values (e.g., 1.5) make the output more random and creative. Use lower values for 
    /// factual/information retrieval tasks and higher values for creative applications.</remarks>
    //[JsonPropertyOrderAttribute(4)]
    //public double? Temperature
    //{
    //    get;
    //    set => field = (value > 0 && value < 2) ? value :
    //            throw new ArgumentOutOfRangeException(nameof(Temperature), "Value must be between 0 and 2 (exclusive)");
    //}

    /// <summary>
    /// Perplexity-Specific: Configuration for using web search in model responses.
    /// </summary>
    //[JsonPropertyName("web_search_options")]
    //[JsonPropertyOrderAttribute(2)]
    //public WebSearchOptions? Web_Search_Options { get; set; }

    //public string ToJson() => JsonSerializer.Serialize(this);
    //public string ToJson(JsonSerializerOptions options) => JsonSerializer.Serialize(this, options);
    //public string ToJson(JsonSerializerOptions options, CustomJsonSerializerOptions customOptions) =>
    //    JsonSerializer.Serialize(this, JsonConfig.Customize(options, customOptions));

    //public override string ToString() => ToJson(JsonSerializerOptions.Default,
    //    CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);

    internal Body()
    {
        Response_Format = new ResponseFormat();
        Max_Tokens = 2000;
        Web_Search_Options = new() { Search_Context_Size = SearchContextSize.Low };
    }
}
