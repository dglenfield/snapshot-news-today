using Microsoft.Data.Sqlite;
using System.Data;

namespace SnapshotJob.Data;

public abstract class SqliteDatabase
{
    public string DatabaseFilePath => _databaseFilePath;

    private readonly string _connectionString;
    private readonly string _databaseFilePath;

    public SqliteDatabase(string databaseFilePath)
    {
        _databaseFilePath = string.IsNullOrWhiteSpace(databaseFilePath) ? throw new ArgumentNullException(nameof(databaseFilePath)) : databaseFilePath;
        _connectionString = $"Data Source={databaseFilePath};Pooling=False";

        // Ensure the directory exists or create it
        var directoryPath = Path.GetDirectoryName(databaseFilePath) ?? throw new DirectoryNotFoundException("Directory path missing or invalid.");
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
    }

    /// <summary>
    /// Executes a non-query SQL command asynchronously against the underlying SQLite database.
    /// </summary>
    /// <param name="commandText">The SQL statement to execute. This should be a valid non-query command such as INSERT, UPDATE, or DELETE.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected by the
    /// command. Note that for commands that do not affect rows (like CREATE TABLE), the return value may be -1.</returns>
    public async Task<int> ExecuteNonQueryAsync(string commandText, SqliteParameter[]? sqliteParameters = null)
    {
        await using SqliteConnection connection = new(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        if (sqliteParameters is not null)
            command.Parameters.AddRange(sqliteParameters);

        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<long> InsertAsync(string commandText, SqliteParameter[]? sqliteParameters = null)
    {
        await using SqliteConnection connection = new(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        if (sqliteParameters is not null)
            command.Parameters.AddRange(sqliteParameters);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();

        // Get the last inserted row ID
        command.CommandText = "SELECT last_insert_rowid();";
        var result = await command.ExecuteScalarAsync();
        return (long)(result ?? -1);
    }

    public async Task<object?> ExecuteScalarAsync(string commandText, SqliteParameter[]? sqliteParameters = null)
    {
        await using SqliteConnection connection = new(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        if (sqliteParameters is not null)
            command.Parameters.AddRange(sqliteParameters);

        await connection.OpenAsync();
        return await command.ExecuteScalarAsync();
    }

    public async Task<SqliteDataReader> ExecuteReaderAsync(string commandText, SqliteParameter[]? sqliteParameters = null)
    {
        SqliteConnection connection = new(_connectionString);
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        if (sqliteParameters is not null)
            command.Parameters.AddRange(sqliteParameters);

        await connection.OpenAsync();
        return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
    }

    /// <summary>
    /// Asynchronously deletes the database file if it exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous delete operation. The task completes when the file has been deleted or
    /// if the file does not exist.</returns>
    public async Task DeleteAsync()
    {
        if (!File.Exists(_databaseFilePath))
            return;
        
        await Task.Delay(100); // Give the OS a moment to release any file lock
        File.Delete(_databaseFilePath);
    }
}
