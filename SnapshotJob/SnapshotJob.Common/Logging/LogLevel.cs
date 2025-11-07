namespace SnapshotJob.Common.Logging;

/// <summary>
/// Specifies the severity level of a log message.
/// </summary>
/// <remarks>Use this enumeration to categorize log entries according to their importance or type. The levels
/// range from detailed debugging information to error conditions. Selecting an appropriate log level helps 
/// filter and manage log output effectively.</remarks>
public enum LogLevel
{
    Debug,
    Info,
    Success,
    Warning,
    Error
}
