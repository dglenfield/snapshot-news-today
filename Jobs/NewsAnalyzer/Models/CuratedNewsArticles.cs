using NewsAnalyzer.Models.PerplexityApi.Common.Response;
using NewsAnalyzer.Serialization;
using System.Text.Json;

namespace NewsAnalyzer.Models;

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

    /// <summary>
    /// Returns a JSON-formatted string that represents the current object.
    /// </summary>
    /// <remarks>The returned JSON string uses default serialization options and omits properties with null
    /// values for readability. This method is useful for logging, debugging, or exporting the object's state.</remarks>
    /// <returns>A string containing the JSON representation of the object, formatted with indentation and excluding properties
    /// with null values.</returns>
    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
