using Common.Logging;
using Common.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Common.Configuration.Options;

public class CustomLoggingOptions
{
    public const string SectionName = "CustomLogging";

    [Required]
    public string ApplicationLogLevel { get; set; } = default!;

    [Required]
    public string LogDirectory { get; set; } = default!;

    [Required]
    public bool LogToFile { get; set; }

    public LogLevel LogLevel => ApplicationLogLevel?.ToLower() switch
    {
        "debug" => LogLevel.Debug,
        "success" => LogLevel.Success,
        "warning" => LogLevel.Warning,
        "error" => LogLevel.Error,
        _ => LogLevel.Info
    };

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
