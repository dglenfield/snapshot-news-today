using SnapshotJob.Common.Serialization;
using SnapshotJob.Data.Models;
using System.Text.Json;

namespace SnapshotJob.Models;

public class ScrapeSectionResult
{
    public required string SectionName { get; set; }
    public int HeadlinesScraped => Headlines.Count;
    public HashSet<ScrapedHeadline> Headlines { get; set; } = [];
    public Exception? Exception { get; set; }

    public bool IsSuccess => Exception is null;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
