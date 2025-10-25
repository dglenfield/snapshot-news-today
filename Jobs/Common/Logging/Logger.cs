using System.Runtime.CompilerServices;

namespace Common.Logging;

public class Logger
{
    private readonly string? _logDirectory;
    private readonly LogLevel _logLevel;
    private readonly string? _logName;
    private readonly bool _logToFile;

    public Logger(LogLevel logLevel, bool logToFile = true, string? logDirectory = null, string? logName = null)
    {
        _logDirectory = logDirectory;
        _logLevel = logLevel;
        _logName = logName;
        _logToFile = logToFile;

        if (_logDirectory is not null)
            Directory.CreateDirectory(_logDirectory);
    }

    public void Log(string message, LogLevel messageLogLevel = LogLevel.Info, bool logAsRawMessage = false, ConsoleColor? consoleColor = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (messageLogLevel < _logLevel) 
            return;

        // Log to the Console
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = messageLogLevel switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Success => ConsoleColor.Green,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => originalColor
        };
        if (consoleColor.HasValue)
            Console.ForegroundColor = consoleColor.Value;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
                
        if (!_logToFile || string.IsNullOrWhiteSpace(_logDirectory))
            return;
        
        // Log to File
        string logEntry = message;
        if (!logAsRawMessage)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string className = Path.GetFileNameWithoutExtension(sourceFilePath);
            logEntry = $"{timestamp} [{messageLogLevel.ToString().ToUpper()}] \t{message}\t <{className}.{memberName}> (Line {sourceLineNumber})";
        }            
        
        try
        {
            string logFileName = string.IsNullOrWhiteSpace(_logName) ? $"log_{DateTime.Now:yyyy-MM-dd}.txt" : $"{_logName}.txt";
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
