namespace NewsScraper.Models.PerplexityApi.Requests.SummarizeArticles;

internal class KeyPointsProperty : PropertyType
{
    public int MinItems { get; set; } = 1;
    public int MaxItems { get; set; } = 5;
    public PropertyType Items { get; set; } = new() { Type = "string" };
}
