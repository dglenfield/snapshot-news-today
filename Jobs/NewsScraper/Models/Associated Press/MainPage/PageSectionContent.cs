using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models.AssociatedPress.MainPage;

public class PageSectionContent
{
    public string? Title { get; set; } // Headline
    public Uri? TargetUri { get; set; }
    public DateTime? LastUpdatedOn { get; set; } // UTC time
    public bool MostRead { get; set; } = false;
    public DateTime? PublishedOn { get; set; } // UTC time
    public string? ScrapeMessage { get; set; } // Error or informational

    public override bool Equals(object? obj)
    {
        return obj is PageSectionContent other && TargetUri?.Equals(other.TargetUri) == true;
    }

    public override int GetHashCode()
    {
        return TargetUri?.GetHashCode() ?? 0;
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
