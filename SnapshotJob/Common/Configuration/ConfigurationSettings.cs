using Common.Configuration.Options;
using Common.Logging;
using Common.Serialization;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Configuration;

public class ConfigurationSettings(IOptions<ApplicationOptions> applicationOptions,
    IOptions<CustomLoggingOptions> customLoggingOptions,
    IOptions<DatabaseOptions> databaseOptions,
    Logger logger)
{
    [JsonPropertyOrder(0)]
    public ApplicationOptions ApplicationOptions => applicationOptions.Value;
    [JsonPropertyOrder(1)]
    public CustomLoggingOptions CustomLoggingOptions => customLoggingOptions.Value;
    [JsonPropertyOrder(2)]
    public DatabaseOptions DatabaseOptions => databaseOptions.Value;

    public void WriteToLog()
    {
        logger.Log("Configuration Settings:", logAsRawMessage: true);
        logger.Log(this.ToString(), logAsRawMessage: true);
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
