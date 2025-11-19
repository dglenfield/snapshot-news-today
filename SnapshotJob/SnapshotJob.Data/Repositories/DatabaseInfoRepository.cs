using Microsoft.Data.Sqlite;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Data.Repositories;

public class DatabaseInfoRepository(SnapshotJobDatabase database)
{
    public async Task CreateTableAsync()
    {
        string commandText = @"
            CREATE TABLE IF NOT EXISTS database_info (
                entity TEXT NOT NULL PRIMARY KEY,
                version TEXT NOT NULL, 
                created_on TEXT NOT NULL DEFAULT (datetime('now')));

            INSERT INTO database_info (entity, version) 
            VALUES ('database_info', '1.1');";

        await database.ExecuteNonQueryAsync(commandText);
    }

    public async Task<DatabaseInfo?> GetAsync(string entity)
    {
        string commandText = "SELECT * FROM database_info WHERE entity = @entity;";
        SqliteParameter[] parameters = [new("@entity", entity)];
        
        await using var reader = await database.ExecuteReaderAsync(commandText, parameters);        
        if (await reader.ReadAsync())
        {
            return new DatabaseInfo()
            {
                Entity = reader.GetString(reader.GetOrdinal("entity")),
                Version = reader.GetString(reader.GetOrdinal("version")),
                CreatedOn = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_on")))
            };
        }
        
        return null;
    }

    public async Task<string> GetDatabaseVersionAsync()
    {
        var result = await database.ExecuteScalarAsync("SELECT version FROM database_info LIMIT 1;");
        return result?.ToString() ?? throw new InvalidOperationException("Database version not found.");
    }

    public async Task<long> InsertAsync(string entity, string version)
    {
        string commandText = @"
            PRAGMA foreign_keys = ON; 
            INSERT INTO database_info (entity, version) 
            VALUES (@entity, @version);";
        SqliteParameter[] parameters = [new("@entity", entity), new("@version", version)];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException($"Insert into database_info failed, no row id returned.");
    }
}
