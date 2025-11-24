using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using SnapshotNewsToday.Data.Configuration.Options;
using System.Net;

namespace SnapshotNewsToday.Data;

public class SnapshotNewsTodayDatabase(IOptions<SnapshotNewsTodayDatabaseOptions> options)
{
    private readonly string _databaseName = options.Value.DatabaseName;
    private readonly string _accountEndpoint = options.Value.AccountEndpoint;
    private readonly string _accountKey = options.Value.AccountKey;

    public async Task CreateDatabase()
    {
        Console.WriteLine($"Connecting to {_accountEndpoint}");
        try
        {
            using CosmosClient client = new(_accountEndpoint, _accountKey);
            var dbResponse = await client.CreateDatabaseIfNotExistsAsync(_databaseName, throughput: 400);
            string status = dbResponse.StatusCode switch
            {
                HttpStatusCode.OK => "exists",
                HttpStatusCode.Created => "created",
                _ => "unknown"
            };
            Console.WriteLine($"Database Id: {dbResponse.Database.Id}, Status: {status}", status == "created" ? ConsoleColor.Green : null);
            Console.WriteLine($"Request Charge = {dbResponse.RequestCharge:N2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public async Task DeleteDatabase()
    {
        Console.WriteLine($"Connecting to {_accountEndpoint}");
        try
        {
            using CosmosClient client = new(_accountEndpoint, _accountKey);
            try
            {
                Database database = client.GetDatabase(_databaseName);
                DatabaseResponse dbResponse = await database.DeleteAsync();
                Console.WriteLine($"Database Id: {dbResponse.Database.Id} DELETED", ConsoleColor.Green);
                Console.WriteLine($"Request Charge = {dbResponse.RequestCharge:N2}");
            }
            catch (Exception)
            {
                Console.WriteLine($"Database Id: {_databaseName} DOES NOT EXIST", ConsoleColor.DarkYellow);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }
}
