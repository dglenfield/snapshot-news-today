namespace NewsScraper.Models;

public class ScrapeNewsJobRun
{
    public long Id { get; set; }
    public required string SourceName { get; set; }
    public required Uri SourceUri { get; set; }
    public DateTime ScrapeStart { get; set; } = default!;
    public DateTime? ScrapeEnd { get; set; }
    public string? RawOutput { get; set; }
}