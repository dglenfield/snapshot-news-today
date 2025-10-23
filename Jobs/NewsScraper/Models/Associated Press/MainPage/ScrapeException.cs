namespace NewsScraper.Models.AssociatedPress.MainPage;

public class ScrapeException
{
    public required string Source { get; set; }
    public Exception Exception { get; set; } = default!;
    
    public string Message => Exception.Message;
}
