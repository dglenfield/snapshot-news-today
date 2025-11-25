using Newtonsoft.Json;

namespace SnapshotNewsToday.Data.Models;

public class Article
{
    [JsonProperty("categoryName")]
    public string? CategoryName { get; set; }

    [JsonProperty("createdOn")]
    public required DateTime CreatedOn { get; set; }

    [JsonProperty("headline")]
    public required string Headline { get; set; }
    
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("isOpinion")]
    public bool? IsOpinion { get; set; }

    [JsonProperty("keyPoints")]
    public List<KeyPoint>? KeyPoints { get; set; }

    [JsonProperty("publishDateId")]
    public required int PublishDateId { get; set; }

    [JsonProperty("source")]
    public string? Source { get; set; }

    [JsonProperty("sourceLink")]
    public string? SourceLink { get; set; }

    [JsonProperty("sourcePublishDate")]
    public DateTime? SourcePublishDate { get; set; }

    [JsonProperty("totalComments")]
    public int TotalComments { get; set; }
}
