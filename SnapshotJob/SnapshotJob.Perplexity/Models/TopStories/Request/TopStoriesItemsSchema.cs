namespace SnapshotJob.Perplexity.Models.TopStories.Request;

/// <summary>
/// Defines the schema for a top story item, specifying the required properties and their types.
/// </summary>
/// <remarks>This schema enforces that each top story item must include the properties "url", "headline",
/// "category", "highlights", and "rationale", all of which are strings. Additional properties beyond these are not
/// permitted.</remarks>
public class TopStoriesItemsSchema : ObjectSchema
{
    /// <summary>
    /// Initializes a new instance of the TopStoriesItemsSchema class with the required properties for a top story item.
    /// </summary>
    /// <remarks>This constructor sets up the schema to require the properties "url", "headline", "category",
    /// "highlights", and "rationale", each as a string. Additional properties are not allowed in this schema.</remarks>
    public TopStoriesItemsSchema()
    {
        Required = ["url", "headline", "category", "highlights", "rationale"];
        //Required = ["id", "rationale"];
        Properties = new Dictionary<string, TypeSchema>
        {
            //["id"] = new TypeSchema { Type = "string" },
            ["url"] = new TypeSchema { Type = "string" },
            ["headline"] = new TypeSchema { Type = "string" },
            ["category"] = new TypeSchema { Type = "string" },
            ["highlights"] = new TypeSchema { Type = "string" },
            ["rationale"] = new TypeSchema { Type = "string" }
        };
        AdditionalProperties = false;
    }
}
