using Microsoft.Extensions.Configuration;
using NewsScraper.Logging;

namespace NewsScraper;

/// <summary>
/// Provides centralized access to application configuration settings, including API endpoints, credentials, logging
/// options, Python integration paths, and test environment values.
/// </summary>
/// <remarks>This static class exposes strongly-typed properties for retrieving configuration values from the
/// application's configuration sources, such as appsettings.json and user secrets. All members are static and throw a
/// KeyNotFoundException if a required configuration key is missing. The class also contains nested static classes for
/// grouping related settings, such as logging, Python integration, and test-specific options. Intended for internal use
/// to ensure consistent and validated access to configuration throughout the application.</remarks>
internal static class Configuration
{
    public static string CnnBaseUrl => _config["NewsProvider:CnnBaseUrl"] ?? throw new KeyNotFoundException("\"NewsProvider:CnnBaseUrl\" not found in appsettings.");
    public static string PerplexityApiKey => _config["PerplexityApiProvider:PerplexityApiKey"] ?? throw new KeyNotFoundException("\"PerplexityApiProvider:PerplexityApiKey\" not found in User Secrets or appsettings.");
    public static string PerplexityApiUrl => _config["PerplexityApiProvider:PerplexityApiUrl"] ?? throw new KeyNotFoundException("\"PerplexityApiProvider:PerplexityApiUrl\" not found in appsettings.");

    /// <summary>
    /// Provides access to logging configuration settings.
    /// </summary>
    /// <remarks>This class exposes static properties for retrieving logging configuration values from the
    /// application's configuration source.</remarks>
    internal static class LoggingSettings
    {
        internal static LogLevel ApplicationLogLevel 
        { 
            get 
            {
                var logLevelSetting = _config["Logging:ApplicationLogLevel:Default"] ?? throw new KeyNotFoundException("\"Logging:ApplicationLogLevel:Default\" not found in appsettings.");
                LogLevel logLevel = logLevelSetting.ToLower() switch
                {
                    "debug" => LogLevel.Debug,
                    "success" => LogLevel.Success,
                    "warning" => LogLevel.Warning,
                    "error" => LogLevel.Error,
                    _ => LogLevel.Info
                };
                return logLevel;
            }
        }
        internal static string LogDirectory => _config["Logging:LogToFile:Directory"] ?? throw new KeyNotFoundException("\"Logging:LogToFile:Directory\" not found in appsettings.");
        internal static bool LogToConsole => bool.Parse(_config["Logging:LogToConsole:Default"] ?? throw new KeyNotFoundException("\"Logging:LogToConsole:Default\" not found in appsettings."));
        internal static bool LogToFile => bool.Parse(_config["Logging:LogToFile:Default"] ?? throw new KeyNotFoundException("\"Logging:LogToFile:Default\" not found in appsettings."));
    }

    /// <summary>
    /// Provides access to configuration settings related to Python integration, including paths to the Python
    /// executable and specific Python scripts.
    /// </summary>
    /// <remarks>This class is intended for internal use to centralize retrieval of Python-related
    /// configuration values from the application's settings. All members are static and require the relevant
    /// configuration keys to be present; otherwise, a KeyNotFoundException is thrown.</remarks>
    internal static class PythonSettings
    {
        internal static string PythonExePath => _config["Python:PythonExePath"] ?? throw new KeyNotFoundException("\"Python:PythonExePath\" not found in appsettings.");
        internal static string GetNewsFromCnnScript => _config["Python:Scripts:GetNewsFromCnn"] ?? throw new KeyNotFoundException("\"Python:Scripts:GetNewsFromCnn\" not found in appsettings.");
    }

    /// <summary>
    /// Provides access to test configuration settings for various providers used during application testing.
    /// </summary>
    /// <remarks>This class contains nested static classes and properties that expose test-specific
    /// configuration values, such as file paths and feature toggles, for use in integration or functional tests. The
    /// settings are typically sourced from the application's configuration files (for example, appsettings.json) and
    /// are intended for internal use within the test infrastructure.</remarks>
    internal static class TestSettings
    {
        internal static class NewsProvider 
        {
            internal static class GetNews 
            {
                internal static bool UseTestLandingPageFile => !_useProductionSettings && bool.Parse(_config["Testing:NewsProvider:GetNews:UseTestLandingPageFile"] ?? throw new KeyNotFoundException("\"Testing:NewsProvider:GetNews:UseTestArticleUrl\" not found in appsettings."));
                internal static string TestLandingPageFile => _config["Testing:NewsProvider:GetNews:TestLandingPageFile"] ?? throw new KeyNotFoundException("\"Testing:NewsProvider:GetNews:TestArticleUrl\" not found in appsettings.");
            }
        }
        internal static class PerplexityApiProvider
        {
            internal static class CurateArticles
            {
                internal static bool UseTestResponseFile => !_useProductionSettings && bool.Parse(_config["Testing:PerplexityApiProvider:CurateArticles:UseTestResponseFile"] ?? throw new KeyNotFoundException("\"Testing:PerplexityApiProvider:CurateArticles:UseTestResponseFile\" not found in appsettings."));
                internal static string TestResponseFile => _config["Testing:PerplexityApiProvider:CurateArticles:TestResponseFile"] ?? throw new KeyNotFoundException("\"Testing:PerplexityApiProvider:CurateArticles:TestResponseFile\" not found in appsettings.");
            }
        }
    }

    private static readonly IConfigurationRoot _config;
    private static readonly bool _useProductionSettings;

    /// <summary>
    /// Initializes the static configuration settings for the application by loading values from the appsettings.json
    /// file and user secrets.
    /// </summary>
    /// <remarks>This static constructor ensures that configuration values are loaded and validated before any
    /// static members of the Configuration class are accessed. If configuration loading fails, the exception is logged
    /// and rethrown.</remarks>
    /// <exception cref="KeyNotFoundException">Thrown if the "UseProductionSettings" key is not found in the appsettings.json file.</exception>
    static Configuration()
    {
        try 
        {
            _config = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<Program>()
            .Build();

            _useProductionSettings = bool.Parse(_config["UseProductionSettings"] ?? throw new KeyNotFoundException("\"UseProductionSettings\" not found in appsettings."));

            // Access ConfigurationSummary to ensure all properties are accessed and validated
            var cs = ConfigurationSummary;
        }
        catch (KeyNotFoundException ex) 
        { 
            Logger.LogException(ex);
            throw;
        }
        catch (Exception ex) 
        { 
            Logger.LogException(ex);
            throw;
        }
    }

    /// <summary>
    /// Logs the current configuration settings to the application log as raw messages.
    /// </summary>
    /// <remarks>Each line of the configuration summary is logged individually. Intended for diagnostic or
    /// troubleshooting purposes. This method is for internal use only.</remarks>
    internal static void LogConfigurationSettings() => ConfigurationSummary.Split('\n').ToList()
        .ForEach(line => Logger.Log(line, logAsRawMessage: true));

    /// <summary>
    /// Gets a formatted summary of the current application configuration settings.
    /// </summary>
    /// <remarks>The summary includes key configuration values for Python integration, news provider
    /// endpoints, Perplexity API credentials, and logging options. If production settings are enabled, test settings
    /// are omitted from the summary. This property is intended for diagnostic or informational purposes.</remarks>
    private static string ConfigurationSummary
    {
        get
        {
            string response = "----- Configuration Settings -----\n";
            response += $"UseProductionSettings = {_useProductionSettings}\n";
            // Python settings
            response += $"PythonSettings:\n";
            response += $"\tPythonExePath = {PythonSettings.PythonExePath}\n";
            response += $"\tGetNewsFromCnnScript = {PythonSettings.GetNewsFromCnnScript}\n";
            // NewsProvider settings
            response += $"NewsProvider:\n";
            response += $"\tCnnBaseUrl = {CnnBaseUrl}\n";
            // PerplexityApiProvider settings
            response += $"PerplexityApiProvider:\n";
            response += $"\tPerplexityApiKey = {PerplexityApiKey[..10]}...\n";
            response += $"\tPerplexityApiUrl = {PerplexityApiUrl}\n";
            // Logging settings
            response += $"LoggingSettings:\n";
            response += $"\tApplicationLogLevel = {LoggingSettings.ApplicationLogLevel}\n";
            response += $"\tLogDirectory = {LoggingSettings.LogDirectory}\n";
            response += $"\tLogToConsole = {LoggingSettings.LogToConsole}\n";
            response += $"\tLogToFile = {LoggingSettings.LogToFile}\n";

            if (_useProductionSettings)
                response += "*** Using production settings, test settings are disabled. ***\n";
            else
            {
                // Test settings
                response += $"TestSettings:\n";
                response += $"\tNewsProvider.GetNews.UseTestArticleUrl = {TestSettings.NewsProvider.GetNews.UseTestLandingPageFile}\n";
                response += $"\tNewsProvider.GetNews.TestArticleUrl = {TestSettings.NewsProvider.GetNews.TestLandingPageFile}\n";
                response += $"\tPerplexityApiProvider.CurateArticles.UseTestResponseFile = {TestSettings.PerplexityApiProvider.CurateArticles.UseTestResponseFile}\n";
                response += $"\tPerplexityApiProvider.CurateArticles.TestResponseFile = {TestSettings.PerplexityApiProvider.CurateArticles.TestResponseFile}\n";
            }

            response += "----- End of Configuration Settings -----\n";
            return response;
        }
    }
}
