using SnapshotNewsToday.Common.Serialization;
using System.Text.Json;

namespace SnapshotJob.Data.Models;

public class PerplexityApiCall
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }

    public double InputTokensCost { get; init; }
    public double OutputTokensCost { get; init; }
    public double RequestCost { get; init; }
    public double TotalCost { get; init; }

    public string? RequestBody { get; set; } 
    public string? ResponseString { get; set; }
    public Exception? Exception { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
