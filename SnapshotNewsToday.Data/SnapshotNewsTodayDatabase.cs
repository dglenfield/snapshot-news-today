using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using SnapshotNewsToday.Data.Configuration.Options;
using SnapshotNewsToday.Data.Models;
using System.Net;

namespace SnapshotNewsToday.Data;

public class SnapshotNewsTodayDatabase(IOptions<SnapshotNewsTodayDatabaseOptions> options)
{
    private readonly string _databaseId = options.Value.DatabaseId;
    private readonly string _accountEndpoint = options.Value.AccountEndpoint;
    private readonly string _accountKey = options.Value.AccountKey;

    public async Task CreateArticle(Article article)
    {
        try
        {
            using CosmosClient client = new(_accountEndpoint, _accountKey);
            Container container = client.GetContainer(_databaseId, "Articles");
            ItemResponse<Article> articleResponse = await container.CreateItemAsync(article);
            Console.WriteLine($"Request Charge = {articleResponse.RequestCharge:N2}");

            Console.WriteLine(articleResponse.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public async Task CreateArticlesContainer()
    {
        Console.WriteLine($"Connecting to {_accountEndpoint}");
        try
        {
            using CosmosClient client = new(_accountEndpoint, _accountKey);
            DatabaseResponse dbResponse = await client.CreateDatabaseIfNotExistsAsync(_databaseId, throughput: 400);
            string status = dbResponse.StatusCode switch
            {
                HttpStatusCode.OK => "exists",
                HttpStatusCode.Created => "created",
                _ => "unknown"
            };
            Console.WriteLine($"Database Id: {dbResponse.Database.Id}, Status: {status}", status == "created" ? ConsoleColor.Green : null);
            Console.WriteLine($"Request Charge = {dbResponse.RequestCharge:N2}");

            // Create Indexing Policy for the container.
            IndexingPolicy indexingPolicy = new()
            {
                Automatic = true,
                IncludedPaths = { new IncludedPath { Path = "/*" } },
                IndexingMode = IndexingMode.Consistent
            };

            ContainerProperties containerProperties = new("Articles", "/publishDateId")
            {
                //IndexingPolicy = indexingPolicy
            };
            var containerResponse = await dbResponse.Database.CreateContainerIfNotExistsAsync(containerProperties, throughput: 400);
            status = containerResponse.StatusCode switch
            {
                HttpStatusCode.OK => "exists",
                HttpStatusCode.Created => "created",
                _ => "unknown"
            };
            Console.WriteLine($"Container Id: {containerResponse.Container.Id}, Status: {status}", status == "created" ? ConsoleColor.Green : null);
            Console.WriteLine($"Request Charge = {containerResponse.RequestCharge:N2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public async Task CreateDatabase()
    {
        Console.WriteLine($"Connecting to {_accountEndpoint}");
        try
        {
            using CosmosClient client = new(_accountEndpoint, _accountKey);
            var dbResponse = await client.CreateDatabaseIfNotExistsAsync(_databaseId, throughput: 400);
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

    public async Task DeleteArticlesContainer()
    {
        Console.WriteLine($"Connecting to {_accountEndpoint}");
        try
        {
            using CosmosClient client = new(_accountEndpoint, _accountKey);
            try
            {
                var database = client.GetDatabase(_databaseId);
                var container = database.GetContainer("Articles");
                var containerResponse = await container.DeleteContainerAsync();
                Console.WriteLine($"Container Id: {containerResponse.Container.Id} DELETED", ConsoleColor.Green);
                Console.WriteLine($"Request Charge = {containerResponse.RequestCharge:N2}");
            }
            catch (Exception)
            {
                Console.WriteLine("Container Id: Articles DOES NOT EXIST", ConsoleColor.DarkYellow);
            }
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
                Database database = client.GetDatabase(_databaseId);
                DatabaseResponse dbResponse = await database.DeleteAsync();
                Console.WriteLine($"Database Id: {dbResponse.Database.Id} DELETED", ConsoleColor.Green);
                Console.WriteLine($"Request Charge = {dbResponse.RequestCharge:N2}");
            }
            catch (Exception)
            {
                Console.WriteLine($"Database Id: {_databaseId} DOES NOT EXIST", ConsoleColor.DarkYellow);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }
}
