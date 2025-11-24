using Microsoft.Extensions.Options;
using SnapshotJob.Common.Logging;
using SnapshotJob.Common.Serialization;
using SnapshotJob.Configuration.Options;
using SnapshotJob.Data.Configuration.Options;
using SnapshotJob.Perplexity.Configuration.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapshotJob.Configuration;

public class ConfigurationSettings(IOptions<ApplicationOptions> applicationOptions,
    IOptions<CustomLoggingOptions> customLoggingOptions,
    IOptions<SnapshotJobDatabaseOptions> databaseOptions,
    IOptions<ScrapingOptions> scrapingOptions,
    IOptions<PerplexityOptions> perplexityOptions,
    Logger logger)
{
    [JsonPropertyOrder(0)]
    public ApplicationOptions ApplicationOptions => applicationOptions.Value;

    [JsonPropertyOrder(1)]
    public CustomLoggingOptions CustomLoggingOptions => customLoggingOptions.Value;

    [JsonPropertyOrder(2)]
    public SnapshotJobDatabaseOptions DatabaseOptions => databaseOptions.Value;

    [JsonPropertyOrder(10)]
    public PerplexityOptions PerplexityOptions => perplexityOptions.Value;

    [JsonPropertyOrder(11)]
    public ScrapingOptions ScrapingOptions => scrapingOptions.Value;

    public void WriteToLog()
    {
        logger.Log("Configuration Settings:", logAsRawMessage: true);
        logger.Log(this.ToString(), logAsRawMessage: true);
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
