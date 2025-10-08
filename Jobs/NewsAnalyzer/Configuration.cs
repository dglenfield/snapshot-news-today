using Common.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace NewsAnalyzer;

/// <summary>
/// Provides centralized access to application configuration settings, including API endpoints, credentials, logging
/// options, Python integration paths, and test environment values.
/// </summary>
/// <remarks>This static class exposes strongly-typed properties for retrieving configuration values from the
/// application's configuration sources, such as appsettings.json and user secrets. All members are static and throw a
/// KeyNotFoundException if a required configuration key is missing. The class also contains nested static classes for
/// grouping related settings, such as logging, Python integration, and test-specific options. Intended for internal use
/// to ensure consistent and validated access to configuration throughout the application.</remarks>
public static class Configuration
{
    public static string PerplexityApiKey => _config["PerplexityApiProvider:PerplexityApiKey"] ?? throw new KeyNotFoundException("\"PerplexityApiProvider:PerplexityApiKey\" not found in User Secrets or appsettings.");
    public static string PerplexityApiUrl => _config["PerplexityApiProvider:PerplexityApiUrl"] ?? throw new KeyNotFoundException("\"PerplexityApiProvider:PerplexityApiUrl\" not found in appsettings.");

    /// <summary>
    /// Provides access to logging configuration settings.
    /// </summary>
    /// <remarks>This class exposes static properties for retrieving logging configuration values from the
    /// application's configuration source.</remarks>
    public static class Logging
    {
        public static LogLevel ApplicationLogLevel 
        { 
            get 
            {
                var logLevelSetting = _config["Logging:ApplicationLogLevel:Default"] ?? throw new KeyNotFoundException("\"Logging:ApplicationLogLevel:Default\" not found in appsettings.");
                return logLevelSetting.ToLower() switch
                {
                    "debug" => LogLevel.Debug,
                    "success" => LogLevel.Success,
                    "warning" => LogLevel.Warning,
                    "error" => LogLevel.Error,
                    _ => LogLevel.Info
                };
            }
        }
        public static string LogDirectory => _config["Logging:LogToFile:Directory"] ?? throw new KeyNotFoundException("\"Logging:LogToFile:Directory\" not found in appsettings.");
        public static bool LogToFile => bool.Parse(_config["Logging:LogToFile:Default"] ?? throw new KeyNotFoundException("\"Logging:LogToFile:Default\" not found in appsettings."));
    }

    /// <summary>
    /// Provides access to test configuration settings for various providers used during application testing.
    /// </summary>
    /// <remarks>This class contains nested static classes and properties that expose test-specific
    /// configuration values, such as file paths and feature toggles, for use in integration or functional tests. The
    /// settings are typically sourced from the application's configuration files (for example, appsettings.json) and
    /// are intended for internal use within the test infrastructure.</remarks>
    public static class TestSettings
    {
        public static class PerplexityApiProvider
        {
            public static class CurateArticles
            {
                public static string TestResponseFile => _config["Testing:PerplexityApiProvider:CurateArticles:TestResponseFile"] ?? throw new KeyNotFoundException("\"Testing:PerplexityApiProvider:CurateArticles:TestResponseFile\" not found in appsettings.");
                public static bool UseTestResponseFile => !_useProductionSettings && bool.Parse(_config["Testing:PerplexityApiProvider:CurateArticles:UseTestResponseFile"] ?? throw new KeyNotFoundException("\"Testing:PerplexityApiProvider:CurateArticles:UseTestResponseFile\" not found in appsettings."));
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
        _config = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<Program>()
            .Build();

        _useProductionSettings = bool.Parse(_config["UseProductionSettings"] ?? throw new KeyNotFoundException("\"UseProductionSettings\" not found in appsettings."));

        ToJson(); // Access ToJson to ensure all properties are accessed and validated
    }

    /// <summary>
    /// Serializes the current configuration settings to a JSON string.
    /// </summary>
    /// <remarks>The returned JSON includes properties from production, logging, Python, and test
    /// settings. Sensitive values, such as API keys, will be partially masked in the output for security.</remarks>
    /// <returns>A JSON-formatted string representing the current values of the configuration settings.</returns>
    public static string ToJson() => JsonSerializer.Serialize(new
    {
        UseProductionSettings = _useProductionSettings, PerplexityApiKey = $"{PerplexityApiKey[..10]}...", PerplexityApiUrl,
        Logging.ApplicationLogLevel, Logging.LogDirectory, Logging.LogToFile,
        TestSettings.PerplexityApiProvider.CurateArticles.UseTestResponseFile, TestSettings.PerplexityApiProvider.CurateArticles.TestResponseFile,
    });
}
