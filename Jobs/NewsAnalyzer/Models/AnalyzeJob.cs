using Common.Models;
using Common.Serialization;
using System.Text.Json;

namespace NewsAnalyzer.Models;

public class AnalyzeJob
{
    public long Id { get; set; }
    public long ScrapeJobId { get; set; }
    public DateTime JobStartedOn { get; } = DateTime.UtcNow;
    public DateTime? JobFinishedOn { get; set; }
    public bool? IsSuccess { get; set; }
    public JobException? AnalyzeJobException { get; set; }

    public decimal? RunTimeInSeconds =>
        JobFinishedOn.HasValue ? (decimal)((long)(JobFinishedOn.Value - JobStartedOn).TotalMilliseconds) / 1000 : null;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
