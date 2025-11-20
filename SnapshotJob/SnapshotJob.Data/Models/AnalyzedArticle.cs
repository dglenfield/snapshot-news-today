using SnapshotJob.Common.Serialization;
using System.Text.Json;

namespace SnapshotJob.Data.Models;

public class AnalyzedArticle
{
    public long Id { get; set; }

    public required long ScrapedArticleId { get; set; }

    public DateTime? AnalyzedOn { get; set; }

    public string CustomHeadline { get; set; } = default!;

    public string Summary { get; set; } = default!;

    public List<string> KeyPoints { get; set; } = [];

    public string KeyPointsJson { get; set; } = default!;

    public Exception? Exception { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
