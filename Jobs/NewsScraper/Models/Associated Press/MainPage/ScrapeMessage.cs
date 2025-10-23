namespace NewsScraper.Models.AssociatedPress.MainPage;

public class ScrapeMessage
{
    public required string Source { get; set; }
    public string Message { get; set; } = default!;
}
