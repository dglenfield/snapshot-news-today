using Common.Data;
using Common.Logging;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace NewsScraper.Data.Providers;

/// <summary>
/// Provides a base implementation for NewsScraper data providers that interact with a SQLite database.
/// </summary>
/// <param name="databaseFilePath">The path to the SQLite database file to be used by the provider. Must not be null, empty, or contain only
/// whitespace.</param>
/// <param name="databaseVersion">The version string representing the expected database schema or data version. Must not be null, empty, or contain
/// only whitespace.</param>
/// <param name="logger">The logger instance used to record diagnostic and operational messages for the provider. Must not be null.</param>
public abstract class BaseDataProvider(string databaseFilePath, string databaseVersion, Logger logger)
    : SqliteDataProvider(databaseFilePath)
{
    protected readonly string _databaseVersion = string.IsNullOrWhiteSpace(databaseVersion) 
        ? throw new ArgumentNullException(nameof(databaseVersion)) : databaseVersion;

    /// <summary>
    /// Asynchronously retrieves the current version string of the database.
    /// </summary>
    /// <returns>A string containing the database version. Throws an exception if the version cannot be determined.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the database version cannot be found.</exception>
    public async Task<string> GetDatabaseVersionAsync()
    {
        var result = await ExecuteScalarAsync("SELECT version FROM database_info LIMIT 1;");
        return result?.ToString() ?? throw new InvalidOperationException("Database version not found.");
    }

    #region Table Creation Method

    /// <summary>
    /// Creates the database_info table if it does not exist and inserts a record containing the current database
    /// version.
    /// </summary>
    /// <remarks>This method enables foreign key constraints before inserting the database version. If the
    /// table already exists, it will not be recreated. Any database errors encountered during execution are logged and
    /// rethrown.</remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the database_info record could not be inserted because no rows were affected.</exception>
    protected async Task CreateDatabaseInfoTable()
    {
        try
        {
            // Create the database_info table if it doesn't exist
            await ExecuteNonQueryAsync(@"
                CREATE TABLE IF NOT EXISTS database_info (
                    version TEXT NOT NULL PRIMARY KEY, 
                    created_on TEXT NOT NULL DEFAULT (datetime('now')));");

            // Insert database_info record with database version
            string commandText = "PRAGMA foreign_keys = ON; INSERT INTO database_info (version) VALUES (@version);";
            SqliteParameter[] parameters = [new("@version", _databaseVersion.Split('-')[0])];
            int affectedRows = await this.ExecuteNonQueryAsync(commandText, parameters);
            if (affectedRows == 0)
                throw new InvalidOperationException("Insert database_info failed, no rows affected.");
        }
        catch (DbException)
        {
            logger.Log($"Error creating the database_info table.", LogLevel.Error);
            throw;
        }
    }

    #endregion
}
