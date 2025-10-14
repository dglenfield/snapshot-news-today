using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models;

/// <summary>
/// Represents the single execution of a scraping job.
/// </summary>
public static class ScrapeJobRun
{
    public static string? ErrorMessage { get; set; }
    public static long Id { get; set; }
    public static int? NewsArticlesScraped { get; set; }
    public static int? NewsArticlesFound { get; set; }
    public static string? RawOutput { get; set; }
    public static DateTime? ScrapeEnd { get; set; }
    public static DateTime ScrapeStart { get; set; } = default!;
    public static string SourceName { get; set; } = default!;
    public static Uri SourceUri { get; set; } = default!;
    public static bool? Success { get; set; }

    /// <summary>
    /// Returns a JSON-formatted string that represents the current state of the object.
    /// </summary>
    /// <remarks>The returned JSON string uses default serialization options and omits properties with null
    /// values for readability. This method is useful for logging, debugging, or exporting the object's state.</remarks>
    /// <returns>A string containing the JSON representation of the object, formatted with indentation and excluding properties
    /// with null values.</returns>
    public static string ToJson()
    {
        var state = new
        {
            ErrorMessage,
            Id,
            NewsArticlesScraped,
            NewsArticlesFound,
            RawOutput,
            ScrapeEnd,
            ScrapeStart,
            SourceName,
            SourceUri,
            Success
        };

        return JsonConfig.ToJson(state, JsonSerializerOptions.Default,
            CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented
        );
    }
}
