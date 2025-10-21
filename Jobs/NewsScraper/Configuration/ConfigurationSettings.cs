using Common.Logging;
using Microsoft.Extensions.Options;
using NewsScraper.Configuration.Options;
using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Configuration;

public class ConfigurationSettings(IOptions<ApplicationOptions> applicationOptions,
    IOptions<CustomLoggingOptions> customLoggingOptions,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<NewsSourceOptions> newsSourceOptions,
    IOptions<PythonOptions> pythonOptions,
    Logger logger)
{
    public ApplicationOptions ApplicationOptions => applicationOptions.Value;
    public CustomLoggingOptions CustomLoggingOptions => customLoggingOptions.Value;
    public DatabaseOptions DatabaseOptions => databaseOptions.Value;
    public NewsSourceOptions NewsSourceOptions => newsSourceOptions.Value;
    public PythonOptions PythonOptions => pythonOptions.Value;

    public void WriteToLog()
    {
        logger.Log("Configuration Settings:", logAsRawMessage: true);
        logger.Log(this.ToString(), logAsRawMessage: true);
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
