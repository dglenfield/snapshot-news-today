using Common.Logging;
using Common.Models.AssociatedPress;
using Common.Serialization;
using System.Text.Json;

namespace Common.Models;

public class NewsSnapshotJob
{
    public long Id { get; set; }
    public DateTime StartedOn { get; } = DateTime.UtcNow;
    public DateTime? FinishedOn { get; set; }
    public bool? IsSuccess { get; set; }
    public JobException? JobException { get; set; }
    public APNewsScrape APNewsScrape { get; set; } = default!;

    public decimal? RunTimeInSeconds =>
        FinishedOn.HasValue ? (decimal)((long)(FinishedOn.Value - StartedOn).TotalMilliseconds) / 1000 : null;

    public void WriteToLog(Logger logger)
    {
        // Job Exception
        if (JobException is not null)
        {
            logger.Log($"JobException from {JobException.Source}", LogLevel.Error, logAsRawMessage: true, ConsoleColor.DarkRed);
            logger.LogException(JobException.Exception);
        }
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
