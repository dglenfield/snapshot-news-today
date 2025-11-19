using Microsoft.Data.Sqlite;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Data.Repositories;

public class DatabaseInfoRepository(SnapshotJobDatabase database)
{
    public float Version { get; } = 1.2F;

    public async Task<DatabaseInfo?> GetAsync(string entity)
    {
        string commandText = "SELECT * FROM database_info WHERE entity = @entity;";
        SqliteParameter[] parameters = [new("@entity", entity)];
        
        await using var reader = await database.ExecuteReaderAsync(commandText, parameters);        
        if (await reader.ReadAsync())
        {
            //string? updatedOn = reader.IsDBNull(3) ? null : reader.GetString(3);

            //Console.WriteLine(updatedOn);
            return new DatabaseInfo()
            {
                Entity = reader.GetString(reader.GetOrdinal("entity")),
                Version = float.Parse(reader.GetString(reader.GetOrdinal("version"))),
                CreatedOn = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_on"))),
                UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) 
                    ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_on")))
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

    public async Task CreateTableAsync()
    {
        string commandText = $@"
            CREATE TABLE IF NOT EXISTS database_info (
                entity TEXT NOT NULL PRIMARY KEY,
                version TEXT NOT NULL, 
                created_on TEXT NOT NULL DEFAULT (datetime('now', 'utc')),
                updated_on TEXT);

            INSERT INTO database_info (entity, version) 
            VALUES ('database_info', '{Version}');";

        await database.ExecuteNonQueryAsync(commandText);
    }

    public bool UpdateTable(float currentVersion)
    {
        Console.WriteLine(currentVersion);
        if (currentVersion == 1.1F)
        {
            string commandText = $@"
                ALTER TABLE database_info ADD COLUMN updated_on TEXT;
                                
                UPDATE database_info SET version = '{currentVersion += .1F}', updated_on = '{DateTime.UtcNow}' 
                WHERE entity = 'database_info';";
            Console.WriteLine(commandText);
            database.ExecuteNonQueryAsync(commandText).Wait();

            return UpdateTable(currentVersion);
        }

        return true; // Table is up to date
    }
}
