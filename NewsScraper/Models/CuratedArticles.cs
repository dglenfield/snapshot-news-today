using NewsScraper.Models.PerplexityApi.Responses;
using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models;

internal class CuratedNewsArticles
{
    public List<CuratedNewsArticle> Articles { get; set; } = default!;

    // PerplexityResponse
    public string PerplexityResponseId { get; set; } = default!;
    public string PerplexityResponseModel { get; set; } = default!;
    public Usage PerplexityApiUsage { get; set; } = default!;
    public string SelectionCriteria { get; set; } = default!;
    public List<string> ExcludedCategoriesList { get; set; } = default!;
    public List<string> Citations { get; set; } = default!;
    public List<SearchResult> SearchResults { get; set; } = default!;

    public string ToJson() => JsonSerializer.Serialize(this);
    public string ToJson(JsonSerializerOptions options) => JsonSerializer.Serialize(this, options);
    public string ToJson(JsonSerializerOptions options, CustomJsonSerializerOptions customOptions) =>
        JsonSerializer.Serialize(this, JsonConfig.Customize(options, customOptions));

    public override string ToString() => ToJson(JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}

internal class CuratedNewsArticle : NewsArticle
{
    public string CuratedHeadline { get; set; } = default!;
    public string Highlights { get; set; } = default!;
    public string Rationale { get; set; } = default!;
    public string CuratedCategory { get; set; } = default!;
}
