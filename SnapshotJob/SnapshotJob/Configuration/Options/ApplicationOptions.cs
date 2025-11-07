using SnapshotJob.Common.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapshotJob.Configuration.Options;

// Root-level settings
public class ApplicationOptions
{
    public const string SectionName = "Application";

    [Required]
    [JsonPropertyOrder(2)]
    public bool LogConfigurationSettings { get; set; }

    [Required]
    [JsonPropertyOrder(0)]
    public string Name { get; set; } = default!;
    
    [Required]
    [JsonPropertyOrder(1)]
    public bool UseProductionSettings { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
