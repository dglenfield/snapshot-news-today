using SnapshotNewsToday.Common.Serialization;
using System.Text.Json;

namespace SnapshotJob.Data.Models;

public class DatabaseInfo
{
    public string Entity { get; set; } = default!;
    public float Version { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
