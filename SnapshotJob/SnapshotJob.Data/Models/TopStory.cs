using SnapshotNewsToday.Common.Serialization;
using System.Text.Json;

namespace SnapshotJob.Data.Models;

public class TopStory
{
    public long Id { get; set; }
    public required long ScrapedArticleId { get; set; }
    public required string Headline { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
