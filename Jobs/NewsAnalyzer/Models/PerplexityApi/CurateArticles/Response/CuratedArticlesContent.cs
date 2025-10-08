using NewsAnalyzer.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsAnalyzer.Models.PerplexityApi.CurateArticles.Response;

/// <summary>
/// Represents the curated articles content returned by the API, including top stories, selection criteria, and excluded
/// categories in both raw and processed forms.
/// </summary>
/// <remarks>This type provides access to both the raw JSON data and convenient strongly-typed properties for
/// working with curated article content in responses. Use the raw properties for advanced scenarios requiring custom parsing or
/// validation. The processed properties offer simplified access to commonly used data such as the list of top stories
/// and excluded category names. All properties are read-only and reflect the data as received from the API.</remarks>
internal class CuratedArticlesContent
{
    /// <summary>
    /// Gets the collection of top stories included in the response.
    /// </summary>
    /// <remarks>The list contains the most prominent stories as determined by the API.</remarks>
    [JsonPropertyName("top_stories")]
    public List<Story> TopStories { get; init; } = default!;

    /// <summary>
    /// Gets the raw selection criteria as a JSON value, which may be either a string or an object depending on the
    /// source data.
    /// </summary>
    /// <remarks>Use this property to access the unprocessed selection criteria for advanced scenarios, such
    /// as custom parsing or validation. The format of the value depends on the input and may require inspection of the
    /// JSON element's type before use.</remarks>
    [JsonPropertyName("selection_criteria")]
    public JsonElement SelectionCriteriaRaw { get; init; }  // Use JsonElement to handle both string and object

    /// <summary>
    /// Gets the raw JSON representation of the excluded categories as received from the source data.
    /// </summary>
    /// <remarks>This property provides direct access to the underlying JSON element for excluded categories.
    /// Consumers are responsible for parsing or interpreting the JSON structure as needed. The format and schema of the
    /// JSON may vary depending on the source.</remarks>
    [JsonPropertyName("excluded_categories")]
    public JsonElement ExcludedCategoriesRaw { get; init; }  // Same for excluded_categories

    // Helper properties to get the actual values
    /// <summary>
    /// Gets the selection criteria as a string representation, regardless of its underlying JSON value type.
    /// </summary>
    /// <remarks>If the underlying JSON value is a string, this property returns its value. Otherwise, it
    /// returns the JSON value serialized as a string. This property is ignored during JSON serialization.</remarks>
    [JsonIgnore]
    public string? SelectionCriteriaText => SelectionCriteriaRaw.ValueKind == JsonValueKind.String ? 
        SelectionCriteriaRaw.GetString() : SelectionCriteriaRaw.ToString();

    /// <summary>
    /// Gets the deserialized selection criteria object if the underlying JSON value represents an object; otherwise,
    /// returns null.
    /// </summary>
    [JsonIgnore]
    public SelectionCriteria? SelectionCriteriaObject =>
        SelectionCriteriaRaw.ValueKind == JsonValueKind.Object ? SelectionCriteriaRaw.Deserialize<SelectionCriteria>() : null;

    /// <summary>
    /// Gets the list of category names that are excluded from processing.
    /// </summary>
    /// <remarks>If no categories are excluded, the returned list is empty. This property is read-only and is
    /// not serialized to JSON.</remarks>
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

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
