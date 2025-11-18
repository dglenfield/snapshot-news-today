using SnapshotJob.Common.Serialization;
using System.Text.Json;

namespace SnapshotJob.Data.Models;

public class TopStoryApiCall
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }

    public double InputTokensCost { get; init; }
    public double OutputTokensCost { get; init; }
    public double RequestCost { get; init; }
    public double TotalCost { get; init; }

    public string ResponseString { get; set; } = default!;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
