using Common.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Common.Configuration.Options;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public bool CreateDatabase { get; set; }

    [Required]
    public string DatabaseVersion { get; set; } = default!;

    [Required]
    public string DirectoryPath { get; set; } = default!;

    [Required]
    public string FileName { get; set; } = default!;

    public string DatabaseFilePath => Path.Combine(DirectoryPath, FileName);

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
