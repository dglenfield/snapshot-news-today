namespace SnapshotNewsToday.Data.Models;

public class Article
{
    public string? CategoryName { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required string Headline { get; set; }
    public required string Id { get; set; }
    public bool? IsOpinion { get; set; }
    public List<KeyPoint>? KeyPoints { get; set; }
    public required int PublishDateId { get; set; }
    public string? Source { get; set; }
    public string? SourceLink { get; set; }
    public DateTime? SourcePublishDate { get; set; }
    public int TotalComments { get; set; }
}
