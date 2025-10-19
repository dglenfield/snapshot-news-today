using Common.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

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
    internal static bool LogConfigurationSettings => bool.Parse(_config["LogConfigurationSettings"] ?? throw new KeyNotFoundException("\"LogConfigurationSettings\" not found in appsettings."));
    internal static bool UseProductionSettings => bool.Parse(_config["UseProductionSettings"] ?? throw new KeyNotFoundException("\"UseProductionSettings\" not found in appsettings."));

    internal static class Database
    {
        internal static class NewsScraperJob
        {
            /// <summary>
            /// Gets the configured version string of the database as specified in the application settings.
            /// </summary>
            /// <remarks>A value followed by -overwrite will delete any existing database and create a new one. 
            /// Use this option with caution as it will result in data loss. Only intended for development or 
            /// testing scenarios. <para>If the key is not found in the configuration, 
            /// a <see cref="KeyNotFoundException"/> is thrown.</para></remarks>
            internal static string DatabaseVersion => _config["Database:NewsScraperJob:DatabaseVersion"] ?? throw new KeyNotFoundException("\"Database:NewsScraperJob:DatabaseVersion\" not found in appsettings.");
            internal static string DirectoryPath => _config["Database:NewsScraperJob:DirectoryPath"] ?? throw new KeyNotFoundException("\"Database:NewsScraperJob:DirectoryPath\" not found in appsettings.");
            internal static string FileName => _config["Database:NewsScraperJob:FileName"] ?? throw new KeyNotFoundException("\"Database:NewsScraperJob:FileName\" not found in appsettings.");
            internal static string DatabaseFilePath => Path.Combine(DirectoryPath, FileName);
        }
    }

    /// <summary>
    /// Provides access to logging configuration settings.
    /// </summary>
    /// <remarks>This class exposes static properties for retrieving logging configuration values from the
    /// application's configuration source.</remarks>
    internal static class Logging
    {
        internal static string LogDirectory => _config["Logging:LogToFile:Directory"] ?? throw new KeyNotFoundException("\"Logging:LogToFile:Directory\" not found in appsettings.");
        internal static LogLevel LogLevel 
        { 
            get 
            {
                var logLevelSetting = _config["Logging:LogLevel:Default"] ?? throw new KeyNotFoundException("\"Logging:LogLevel:Default\" not found in appsettings.");
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
        internal static bool LogToFile => bool.Parse(_config["Logging:LogToFile:Default"] ?? throw new KeyNotFoundException("\"Logging:LogToFile:Default\" not found in appsettings."));
    }

    internal static class NewsSourceUrls
    {
        internal static string AssociatedPressBaseUrl => _config["NewsSourceUrls:AssociatedPressBaseUrl"] ?? throw new KeyNotFoundException("\"NewsSourceUrls:AssociatedPressBaseUrl\" not found in appsettings.");
        internal static string CnnBaseUrl => _config["NewsSourceUrls:CnnBaseUrl"] ?? throw new KeyNotFoundException("\"NewsSourceUrls:CnnBaseUrl\" not found in appsettings.");
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
        internal static string GetNewsFromCnnScript => _config["Python:Scripts:GetNewsFromCnn"] ?? throw new KeyNotFoundException("\"Python:Scripts:GetNewsFromCnn\" not found in appsettings.");
        internal static string PythonExePath => _config["Python:PythonExePath"] ?? throw new KeyNotFoundException("\"Python:PythonExePath\" not found in appsettings.");
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
        internal static class NewsArticleProvider
        {
            internal static class GetArticle
            {
                internal static string TestArticleFile => _config["Testing:NewsArticleProvider:GetArticle:TestArticleFile"] ?? throw new KeyNotFoundException("\"Testing:NewsArticleProvider:GetArticle:TestArticleFile\" not found in appsettings.");
                internal static bool UseTestArticleFile => !UseProductionSettings && bool.Parse(_config["Testing:NewsArticleProvider:GetArticle:UseTestArticleFile"] ?? throw new KeyNotFoundException("\"Testing:NewsArticleProvider:GetArticle:UseTestArticleFile\" not found in appsettings."));
            }            
        }
        internal static class NewsStoryProvider 
        {
            internal static class GetNews 
            {
                internal static string TestLandingPageFile => _config["Testing:NewsStoryProvider:GetNews:TestLandingPageFile"] ?? throw new KeyNotFoundException("\"Testing:NewsStoryProvider:GetNews:TestArticleUrl\" not found in appsettings.");
                internal static bool UseTestLandingPageFile => !UseProductionSettings && bool.Parse(_config["Testing:NewsStoryProvider:GetNews:UseTestLandingPageFile"] ?? throw new KeyNotFoundException("\"Testing:NewsStoryProvider:GetNews:UseTestArticleUrl\" not found in appsettings."));
            }
        }
    }

    private static readonly IConfigurationRoot _config = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<Program>()
            .Build();

    /// <summary>
    /// Initializes static members of the Configuration class and performs validation of configuration properties.
    /// </summary>
    /// <remarks>This static constructor ensures that all configuration properties are accessed and validated
    /// when the class is first used. This helps detect misconfigurations early in the application's
    /// lifecycle.</remarks>
    static Configuration()
    {
        ToJson(); // Access ToJson to ensure all properties are accessed and validated
    }

    /// <summary>
    /// Serializes the current configuration settings to a JSON string.
    /// </summary>
    /// <remarks>The returned JSON includes properties from production, logging, Python, and test
    /// settings. Sensitive values, such as API keys, will be partially masked in the output for security.</remarks>
    /// <returns>A JSON-formatted string representing the current values of the configuration settings.</returns>
    internal static string ToJson() => JsonSerializer.Serialize(new
    {
        LogConfigurationSettings,
        UseProductionSettings,
        Database = new
        {
            NewsScraperJob = new
            {
                Database.NewsScraperJob.DatabaseFilePath,
                Database.NewsScraperJob.DatabaseVersion,
                Database.NewsScraperJob.DirectoryPath,
                Database.NewsScraperJob.FileName
            }
        },
        Logging = new
        {
            Logging.LogDirectory,
            Logging.LogLevel,
            Logging.LogToFile
        },
        NewsSourceUrls = new
        {
            NewsSourceUrls.AssociatedPressBaseUrl,
            NewsSourceUrls.CnnBaseUrl
        },
        PythonSettings = new
        {
            PythonSettings.GetNewsFromCnnScript,
            PythonSettings.PythonExePath
        },
        TestSettings = new
        {
            NewsArticleProvider = new
            {
                GetArticle = new
                {
                    TestSettings.NewsArticleProvider.GetArticle.TestArticleFile,
                    TestSettings.NewsArticleProvider.GetArticle.UseTestArticleFile
                }
            },
            NewsStoryProvider = new
            {
                GetNews = new
                {
                    TestSettings.NewsStoryProvider.GetNews.TestLandingPageFile,
                    TestSettings.NewsStoryProvider.GetNews.UseTestLandingPageFile,
                }
            }
        }
    }, new JsonSerializerOptions { WriteIndented = true });
}
