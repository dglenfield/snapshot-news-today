using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Responses.CurateArticles;

internal class SelectionCriteria
{
    [JsonPropertyName("national_international_impact")]
    public string NationalInternationalImpact { get; set; } = default!;

    [JsonPropertyName("immediacy")]
    public string Immediacy { get; set; } = default!;

    [JsonPropertyName("societal_relevance")]
    public string SocietalRelevance { get; set; } = default!;

    [JsonPropertyName("shaping_upcoming_events")]
    public string ShapingUpcomingEvents { get; set; } = default!;
}
