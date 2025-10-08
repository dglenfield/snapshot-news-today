using System.Runtime.CompilerServices;

namespace Common.Logging;

/// <summary>
/// Provides static methods for logging messages and exceptions to configured output destinations, such as console and
/// log files, using application-defined log levels.
/// </summary>
/// <remarks>The Logger class reads its configuration from the application's settings file and user secrets during
/// static initialization. It supports logging to both the console and files, with log entries formatted to include
/// timestamps, severity levels, and source context. Console output uses color coding based on log severity. For thread
/// safety when logging from multiple threads, additional synchronization may be required. Exceptions encountered during
/// logging are written to the console and rethrown. This class is intended for internal use within the
/// application.</remarks>
public class Logger
{
    private readonly LogLevel _applicationLogLevel;
    private readonly string _logDirectory;
    private readonly bool _logToFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="Logger"/> class with specified logging options.
    /// </summary>
    /// <param name="applicationLogLevel">The default log level for the application.</param>
    /// <param name="logDirectory">The directory where log files will be created. Must exist or be creatable.</param>
    /// <param name="logToFile">Specifies whether logging to a file is enabled.</param>
    /// <exception cref="InvalidOperationException">Thrown if file logging is enabled but the log file path is not specified in the configuration.</exception>
    public Logger(LogLevel applicationLogLevel, string logDirectory, bool logToFile)
    {
        _applicationLogLevel = applicationLogLevel;
        _logDirectory = logDirectory;
        _logToFile = logToFile;

        if (_logToFile)
            Directory.CreateDirectory(_logDirectory);
    }

    /// <summary>
    /// Writes a log entry with the specified message and log level to the configured output destinations.
    /// </summary>
    /// <remarks>If logging to a file is enabled, the log entry includes a timestamp, log level, message, and
    /// source context. Logging to the console uses color coding based on the log level. Exceptions encountered during
    /// file logging are written to the console if enabled and then rethrown. For thread safety when logging from
    /// multiple threads, additional synchronization may be required.</remarks>
    /// <param name="message">The message to log. This should describe the event or information to be recorded.</param>
    /// <param name="level">The severity level of the log entry. Determines whether the message is logged based on the application's current
    /// log level. Defaults to <see cref="LogLevel.Info"/>.</param>
    /// <param name="logFileName">The name of the log file to write to. If <see langword="null"/> or whitespace, a default file name is generated
    /// based on the current date.</param>
    /// <param name="memberName">The name of the calling member. Automatically supplied by the compiler; do not specify explicitly.</param>
    /// <param name="sourceFilePath">The full file path of the source code that invoked the method. Automatically supplied by the compiler; do not
    /// specify explicitly.</param>
    /// <param name="sourceLineNumber">The line number in the source file where the method was called. Automatically supplied by the compiler; do not
    /// specify explicitly.</param>
    public void Log(string message, LogLevel level = LogLevel.Info, string? logFileName = null, 
        bool logAsRawMessage = false,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (level < _applicationLogLevel) 
            return;

        // Console logging
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Success => ConsoleColor.Green,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => originalColor
        };
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
                
        if (!_logToFile)
            return;
        
        // File logging
        string logEntry = message;
        if (!logAsRawMessage)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string className = Path.GetFileNameWithoutExtension(sourceFilePath);
            logEntry = $"{timestamp} [{level.ToString().ToUpper()}] \t{message} <{className}.{memberName}> (Line {sourceLineNumber})";
        }            
        
        if (string.IsNullOrWhiteSpace(logFileName))
            logFileName = $"{logFileName ?? "log"}_{DateTime.Now:yyyy-MM-dd}.txt";
        try
        {
            // TODO: For thread safety, use StreamWriter with lock if logging from multiple threads.
            File.AppendAllText(Path.Combine(_logDirectory, logFileName), $"{logEntry}\n");
        }
        catch (Exception ex)
        {
            // If file logging fails, write the error to console, then rethrow.
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"{ex.GetType()}: {ex.Message}\nStackTrace: {ex.StackTrace}");
            Console.ForegroundColor = originalColor;
            throw;
        }
    }

    /// <summary>
    /// Logs the specified exception to the error output and records it as an error in the application's log.
    /// </summary>
    /// <remarks>The exception message is written to the standard error stream in red text for visibility. The
    /// full exception details, including the stack trace, are recorded using the application's logging mechanism at the
    /// error level.</remarks>
    /// <param name="ex">The exception to be logged. Cannot be null.</param>
    public void LogException(Exception ex) => 
        Log($"{ex.GetType()}: {ex.Message}\nStackTrace: {ex.StackTrace}", LogLevel.Error);
}
