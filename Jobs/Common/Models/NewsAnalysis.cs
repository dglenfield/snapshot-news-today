using Common.Serialization;
using System.Text.Json;

namespace Common.Models;

public class NewsAnalysis
{
    public long Id { get; set; }
    public DateTime StartedOn { get; } = DateTime.UtcNow;
    public DateTime? FinishedOn { get; set; }
    public bool? IsSuccess { get; set; }
    public JobException? NewsAnalysisException { get; set; }

    public decimal? RunTimeInSeconds =>
        FinishedOn.HasValue ? (decimal)((long)(FinishedOn.Value - StartedOn).TotalMilliseconds) / 1000 : null;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
