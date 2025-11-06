using Common.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Models.Scraping;

public class ScrapedArticle
{
    public long Id { get; set; }

    public required long ScrapedHeadlineId { get; set; }

    public DateTime? ScrapedOn { get; set; }

    public string? Headline { get; set; }

    public Uri SourceUri { get; set; } = default!;

    public string? TestFile { get; set; }

    public DateTime? LastUpdatedOn { get; set; } // UTC time

    public string? Author { get; set; }

    public List<string>? ContentParagraphs { get; set; }

    public bool IsSuccess { get; set; } = false;

    [JsonIgnore]
    public List<JobException>? ScrapeExceptions { get; set; }

    [JsonIgnore]
    public string? Content => ContentParagraphs?.Count > 0 ? string.Join("\n\n", ContentParagraphs) : null;

    public string GetContentParagraphs(int maxParagraphs)
    {
        if (maxParagraphs <= 0 || ContentParagraphs?.Count == 0)
            return string.Empty;

        StringBuilder builder = new();
        for (int i = 0; i < maxParagraphs && i < ContentParagraphs?.Count; i++)
            builder.AppendLine($"  [{i + 1}] {ContentParagraphs[i]}");

        return builder.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is ScrapedArticle other && SourceUri?.Equals(other.SourceUri) == true;
    }

    public override int GetHashCode()
    {
        return SourceUri?.GetHashCode() ?? 0;
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
