using Common.Serialization;
using System.Text.Json;

namespace SnapshotJob.Data.Models;

public class NewsSnapshot
{
    public long Id { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? FinishedOn { get; set; }
    public bool? IsSuccess { get; set; }
    public Exception? SnapshotException { get; set; }
    public List<Exception>? ScrapeExceptions { get; set; }
    public int ArticlesScraped { get; set; }
    public int HeadlinesScraped { get; set; }
    public int SectionsScraped { get; set; }

    public decimal? RunTimeInSeconds => StartedOn.HasValue && FinishedOn.HasValue ? 
        (decimal)((long)(FinishedOn.Value - StartedOn.Value).TotalMilliseconds) / 1000 : null;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
