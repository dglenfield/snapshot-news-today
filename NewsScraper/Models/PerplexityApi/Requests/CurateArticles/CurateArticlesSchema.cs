namespace NewsScraper.Models.PerplexityApi.Requests.CurateArticles;

internal class Schema : Requests.Schema
{
    internal Schema() : base(type: "array")
    {
        MinItems = 10;
        MaxItems = 10;

        Items = new SchemaItem()
        {
            Type = "object",
            Required = ["url", "headline", "category", "highlights", "rationale"],
            Properties = new Dictionary<string, PropertyType>
            {
                { "url", new PropertyType { Type = "string", Description = "The URL of the news article" } },
                { "headline", new PropertyType { Type = "string", Description = "A catchy headline for the article" } },
                { "category", new PropertyType { Type = "string", Description = "The category of the news article (e.g., politics, economy, health)" } },
                { "highlights", new PropertyType { Type = "string", Description = "Key highlights about the news story this article is about" } },
                { "rationale", new PropertyType { Type = "string", Description = "A short explanation of why this news story is important" } }
            },
            AdditionalProperties = false
        };
    }
}

internal class StrictSchema : Requests.Schema
{
    internal StrictSchema() : base(type: "object") 
    { 
        Required = ["top_stories", "selection_criteria", "excluded_categories"];
        Properties = [];
        AdditionalProperties = false;
    }
}

internal class Schema2 : Requests.Schema
{
    //public PropertyType[]? Properties { get; set; }

    internal Schema2() : base(type: "object")
    {
        string systemContent = @"
You are a news analyst AI assistant.
Your task is to select the 10 most important news stories from a provided list of URLs.

CRITICAL: You MUST return ONLY valid JSON matching this exact structure:
{
  ""top_stories"": [ /* array of exactly 10 story objects */ ],
  ""selection_criteria"": ""string explanation"",
  ""excluded_categories"": [ /* array of strings */ ]
}

Each story object must have: url, headline, category, highlights, rationale.
Do NOT add any additional fields or vary the structure.
Do NOT wrap the JSON in markdown code blocks.
Return only the raw JSON object.";

        // Make your schema stricter
        var schema = new
        {
            type = "object",
            required = new[] { "top_stories", "selection_criteria", "excluded_categories" },
            properties = new
            {
                top_stories = new  // Exact name, not top_10_stories
                {
                    type = "array",
                    minItems = 10,
                    maxItems = 10,
                    items = new
                    {
                        type = "object",
                        required = new[] { "url", "headline", "category", "highlights", "rationale" },
                        properties = new
                        {
                            url = new { type = "string" },
                            headline = new { type = "string" },
                            category = new { type = "string" },
                            highlights = new { type = "string" },
                            rationale = new { type = "string" }
                        },
                        additionalProperties = false
                    }
                },
                selection_criteria = new { type = "string" },  // Or define as object with specific properties
                excluded_categories = new
                {
                    type = "array",
                    items = new { type = "string" }
                }
            },
            additionalProperties = false  // Critical for consistency
        };
    }
}