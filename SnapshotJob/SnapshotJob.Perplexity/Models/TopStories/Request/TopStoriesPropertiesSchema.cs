using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Request;

/// <summary>
/// Represents the response schema definitions for selection criteria, excluded categories, 
/// and top stories used in content curation.
/// </summary>
public class TopStoriesPropertiesSchema
{
    /// <summary>
    /// Gets the collection of top stories associated with this instance.
    /// </summary>
    [JsonPropertyName("top_stories")]
    public ArraySchema TopStories { get; init; }

    /// <summary>
    /// Gets the selection criteria associated with this instance.
    /// </summary>
    [JsonPropertyName("selection_criteria")]
    public TypeSchema SelectionCriteria { get; init; }

    /// <summary>
    /// Gets the collection of categories excluded from curation.
    /// </summary>
    [JsonPropertyName("excluded_categories")]
    public ArraySchema ExcludedCategories { get; init; }

    /// <summary>
    /// Initializes a new instance of the SchemaProperties class with default schema definitions for selection criteria,
    /// excluded categories, and top stories.
    /// </summary>
    public TopStoriesPropertiesSchema()
    {
        // Define the schema for selection criteria
        SelectionCriteria = new TypeSchema { Type = "string" };

        // Define the schema for excluded categories
        ExcludedCategories = new ArraySchema { Items = new TypeSchema { Type = "string" } };

        // Define the schema for top stories
        TopStories = new ArraySchema
        {
            MinItems = 10,
            MaxItems = 10,
            Items = new TopStoriesItemsSchema()
        };   
    }
}
