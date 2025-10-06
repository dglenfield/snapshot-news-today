using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.CurateArticles.Response;

/// <summary>
/// Represents a set of criteria used to evaluate national or international impact, immediacy, societal relevance, and
/// upcoming events for curating the news articles.
/// </summary>
internal class SelectionCriteria
{
    /// <summary>
    /// Gets the description of the impact at the national or international level.
    /// </summary>
    [JsonPropertyName("national_international_impact")]
    public string NationalInternationalImpact { get; init; } = default!;

    /// <summary>
    /// Gets the immediacy level associated with the object.
    /// </summary>
    [JsonPropertyName("immediacy")]
    public string Immediacy { get; init; } = default!;

    /// <summary>
    /// Gets the description of the societal relevance associated with the entity.
    /// </summary>
    [JsonPropertyName("societal_relevance")]
    public string SocietalRelevance { get; init; } = default!;

    /// <summary>
    /// Gets the description of upcoming events shaping the context.
    /// </summary>
    [JsonPropertyName("shaping_upcoming_events")]
    public string ShapingUpcomingEvents { get; init; } = default!;
}
