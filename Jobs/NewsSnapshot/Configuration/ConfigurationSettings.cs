using Common.Configuration.Options;
using Common.Logging;
using Common.Serialization;
using Microsoft.Extensions.Options;
using NewsSnapshot.Configuration.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsSnapshot.Configuration;

public class ConfigurationSettings(IOptions<ApplicationOptions> applicationOptions,
    IOptions<CustomLoggingOptions> customLoggingOptions,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<NewsSourceOptions> newsSourceOptions,
    IOptions<PerplexityOptions> perplexityOptions,
    IOptions<PythonOptions> pythonOptions,
    Logger logger) : Common.Configuration.ConfigurationSettings(applicationOptions, customLoggingOptions, databaseOptions, logger)
{

    [JsonPropertyOrder(10)]
    public NewsSourceOptions NewsSourceOptions => newsSourceOptions.Value;

    [JsonPropertyOrder(11)]
    public PerplexityOptions PerplexityOptions => perplexityOptions.Value;

    [JsonPropertyOrder(12)]
    public PythonOptions PythonOptions => pythonOptions.Value;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
