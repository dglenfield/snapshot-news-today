namespace NewsScraper.Models.PerplexityApi.CurateArticles.Request;

/// <summary>
/// Represents the root JSON schema definition for the response object.
/// </summary>
internal class JsonSchema
{
    /// <summary>
    /// Gets the name of the root schema.
    /// </summary>
    public string Name => "top_stories";

    /// <summary>
    /// Gets the schema definition for top stories objects.
    /// </summary>
    public TopStoriesSchema Schema { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether strict mode is enabled.
    /// </summary>
    public bool Strict => true;
}
