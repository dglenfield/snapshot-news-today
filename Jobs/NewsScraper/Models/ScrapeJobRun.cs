namespace NewsScraper.Models;

/// <summary>
/// Represents the single execution of a scraping job.
/// </summary>
public static class ScrapeJobRun
{
    public static string? ErrorMessage { get; set; }
    public static long Id { get; set; }
    public static int? NewsStoriesFound { get; set; }
    public static int? NewsArticlesScraped { get; set; }
    public static string? RawOutput { get; set; }
    public static DateTime? ScrapeEnd { get; set; }
    public static DateTime ScrapeStart { get; set; } = default!;
    public static string SourceName { get; set; } = default!;
    public static Uri SourceUri { get; set; } = default!;
    public static bool? Success { get; set; }
}
