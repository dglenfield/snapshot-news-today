namespace Common.Data.Repositories;

public class NewsAnalysisRepository(NewsSnapshotDatabase database)
{
    public async Task CreateTableAsync()
    {
        string script = "CreateNewsAnalysisTableV1.1";
        string scriptFilePath = Path.Combine(AppContext.BaseDirectory, "Data\\Scripts", script);
        string scriptContent = File.ReadAllText(scriptFilePath);

        await database.ExecuteNonQueryAsync(scriptContent);
    }
}
