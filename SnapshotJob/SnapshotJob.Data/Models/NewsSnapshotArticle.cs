using SnapshotJob.Common.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapshotJob.Data.Models;

public class NewsSnapshotArticle
{
    public long Id { get; set; }
    public required long NewsSnapshotId { get; init; }
    public required long AnalyzedArticleId { get; init; }

    public DateTime? CreatedOn { get; set; } // UTC time
    public DateTime? PublishedOn { get; set; } // UTC time

    public required Uri SourceUri { get; init; }
    public required string SourceHeadline { get; init; }
    public string? CustomHeadline { get; set; }
    
    public string? SourceSectionName { get; set; }

    public DateTime? LastUpdatedOn { get; set; } // UTC time
    public string? Author { get; set; }

    public string? Summary { get; set; }
    public List<string>? KeyPoints { get; set; }
    public string? KeyPointsJson { get; set; }

    public List<string>? ContentParagraphs { get; set; }

    [JsonIgnore]
    public string? Content => ContentParagraphs?.Count > 0 ? string.Join("\n\n", ContentParagraphs) : null;

    [JsonIgnore]
    public bool IsPublished => PublishedOn != null;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
