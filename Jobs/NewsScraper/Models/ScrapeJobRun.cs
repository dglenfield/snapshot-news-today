namespace NewsScraper.Models;

public static class ScrapeJobRun
{
    public static long Id { get; set; }
    public static string SourceName { get; set; } = default!;
    public static Uri SourceUri { get; set; } = default!;
    public static int? SourceArticlesFound { get; set; }
    public static DateTime ScrapeStart { get; set; } = default!;
    public static DateTime? ScrapeEnd { get; set; }
    public static string? RawOutput { get; set; }
    public static bool? Success { get; set; }
    public static string? ErrorMessage { get; set; }
}
