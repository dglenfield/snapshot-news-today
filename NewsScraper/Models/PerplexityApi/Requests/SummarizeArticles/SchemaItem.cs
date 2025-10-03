namespace NewsScraper.Models.PerplexityApi.Requests.SummarizeArticles;

internal class SchemaItem
{
    public string Type { get; set; } = "object";
    public string[] Required { get; set; } = ["custom_headline", "summary", "key_points"];
    public SchemaProperties? Properties { get; set; } = new SchemaProperties();
    public bool? AdditionalProperties { get; set; } = false;
}

internal class SchemaProperties
{
    public PropertyType Custom_Headline { get; set; } = new() { Type = "string", Description = "Custom headline" };
    public PropertyType Summary { get; set; } = new() { Type = "string", Description = "Brief summary" };
    public KeyPointsProperty Key_Points { get; set; }
}
