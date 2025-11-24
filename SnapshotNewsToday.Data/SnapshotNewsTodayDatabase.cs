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
}
