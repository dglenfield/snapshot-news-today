using Common.Configuration.Options;
using Common.Logging;
using Common.Serialization;
using Microsoft.Extensions.Options;
using NewsAnalyzer.Configuration.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsAnalyzer.Configuration;

public class ConfigurationSettings(IOptions<ApplicationOptions> applicationOptions,
    IOptions<CustomLoggingOptions> customLoggingOptions,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<PerplexityOptions> perplexityOptions,
    Logger logger) : Common.Configuration.ConfigurationSettings(applicationOptions, customLoggingOptions, databaseOptions, logger)
{
    [JsonPropertyOrder(10)]
    public PerplexityOptions PerplexityOptions => perplexityOptions.Value;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
