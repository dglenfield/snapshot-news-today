using SnapshotJob.Common.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SnapshotJob.Data.Configuration.Options;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public bool DeleteExistingDatabase { get; set; }

    [Required]
    public string DirectoryPath { get; set; } = default!;

    [Required]
    public string FileName { get; set; } = default!;

    public string DatabaseFilePath => Path.Combine(DirectoryPath, FileName);

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
