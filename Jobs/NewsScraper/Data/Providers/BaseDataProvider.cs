using Common.Data;
using Common.Logging;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using NewsScraper.Configuration.Options;
using System.Data.Common;

namespace NewsScraper.Data.Providers;

public abstract class BaseDataProvider(Logger logger, IOptions<DatabaseOptions> databaseOptions)
    : SqliteDataProvider(databaseOptions.Value.NewsScraperJob.DatabaseFilePath)
{
    private readonly DatabaseOptions _databaseOptions = databaseOptions.Value;    

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
            SqliteParameter[] parameters = [new("@version", _databaseOptions.NewsScraperJob.DatabaseVersion.Split('-')[0])];
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
