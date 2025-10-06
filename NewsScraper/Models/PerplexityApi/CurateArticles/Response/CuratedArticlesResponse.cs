using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.CurateArticles.Response;

internal class CuratedArticlesResponse
{
    [JsonPropertyName("top_stories")]
    public List<Story> TopStories { get; set; } = default!;

    [JsonPropertyName("selection_criteria")]
    public JsonElement SelectionCriteriaRaw { get; set; }  // Use JsonElement to handle both string and object

    [JsonPropertyName("excluded_categories")]
    public JsonElement ExcludedCategoriesRaw { get; set; }  // Same for excluded_categories

    // Helper properties to get the actual values
    [JsonIgnore]
    public string? SelectionCriteriaText => SelectionCriteriaRaw.ValueKind == JsonValueKind.String ? 
        SelectionCriteriaRaw.GetString() : SelectionCriteriaRaw.ToString();

    [JsonIgnore]
    public SelectionCriteria? SelectionCriteriaObject =>
        SelectionCriteriaRaw.ValueKind == JsonValueKind.Object ? SelectionCriteriaRaw.Deserialize<SelectionCriteria>() : null;

    [JsonIgnore]
    public List<string>? ExcludedCategoriesList
    {
        get
        {
            if (ExcludedCategoriesRaw.ValueKind == JsonValueKind.Array)
                return ExcludedCategoriesRaw.Deserialize<List<string>>();
            
            if (ExcludedCategoriesRaw.ValueKind == JsonValueKind.Object)
            {
                // Handle object case from previous response
                var obj = ExcludedCategoriesRaw.Deserialize<Dictionary<string, string>>();
                return obj?.Values.ToList() ?? new List<string>();
            }

            return [];
        }
    }
}
