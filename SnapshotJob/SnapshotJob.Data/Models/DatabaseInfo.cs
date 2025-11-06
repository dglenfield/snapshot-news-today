using Common.Serialization;
using System.Text.Json;

namespace SnapshotJob.Data.Models;

public class DatabaseInfo
{
    public DateTime CreatedOn { get; set; }
    public string Entity { get; set; } = default!;
    public string Version { get; set; } = default!;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
