using NewsScraper.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NewsScraper.Configuration.Options;

public class PythonOptions
{
    public const string SectionName = "Python";

    [Required]
    public string PythonExePath { get; set; } = string.Empty;

    [Required]
    public ScriptsOptions Scripts { get; set; } = new();
    public class ScriptsOptions
    {
        [Required]
        public string GetNewsFromCnn { get; set; } = string.Empty;
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
