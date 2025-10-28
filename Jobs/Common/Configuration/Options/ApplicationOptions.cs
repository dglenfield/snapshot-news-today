using Common.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Common.Configuration.Options;

// Root-level settings
public class ApplicationOptions
{
    public const string SectionName = "Application";

    [Required]
    public bool LogConfigurationSettings { get; set; }

    [Required]
    public string Name { get; set; } = default!;
    
    [Required]
    public bool UseProductionSettings { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
