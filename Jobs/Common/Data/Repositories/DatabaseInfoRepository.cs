using Common.Models;
using Microsoft.Data.Sqlite;

namespace Common.Data.Repositories;

public class DatabaseInfoRepository(NewsSnapshotDatabase database)
{
    public async Task CreateTableAsync()
    {
        string script = "CreateDatabaseInfoTableV1.1";
        string scriptFilePath = Path.Combine(AppContext.BaseDirectory, "Data\\Scripts", script);
        string scriptContent = File.ReadAllText(scriptFilePath);

        await database.ExecuteNonQueryAsync(scriptContent);
    }

    public async Task<DatabaseInfo> GetAsync(string entity)
    {
        string commandText = $"SELECT * FROM database_info WHERE entity = @entity;";
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
        
        throw new InvalidOperationException("Database version not found.");
    }

    public async Task<string> GetDatabaseVersionAsync()
    {
        var result = await database.ExecuteScalarAsync("SELECT version FROM database_info LIMIT 1;");
        return result?.ToString() ?? throw new InvalidOperationException("Database version not found.");
    }

    public async Task<long> InsertAsync(string entity, string version)
    {
        string commandText = $@"
            PRAGMA foreign_keys = ON; 
            INSERT INTO database_info (entity, version) 
            VALUES (@entity, @version);";

        SqliteParameter[] parameters = [new("@entity", entity), new("@version", version)];

        long id = await database.InsertAsync(commandText, parameters);
        return id > 0 ? id : throw new InvalidOperationException($"Insert into database_info failed, no row id returned.");
    }
}
