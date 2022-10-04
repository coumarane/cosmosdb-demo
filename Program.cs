using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;
using Bogus;

using IHost host = Host.CreateDefaultBuilder(args).Build();

// Ask the service provider for the configuration abstraction.
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

string accountEndpoint = config.GetValue<string>("accountEndpoint");
string authKeyOrResourceToken = config.GetValue<string>("authKeyOrResourceToken");

#region Application code should start here.

// See https://aka.ms/new-console-template for more information

// New instance of CosmosClient class
// using CosmosClient client = new(
//     accountEndpoint: Environment.GetEnvironmentVariable("COSMOS_ENDPOINT")!,
//     authKeyOrResourceToken: Environment.GetEnvironmentVariable("COSMOS_KEY")!
// );

using CosmosClient client = new(
    accountEndpoint: accountEndpoint,
    authKeyOrResourceToken: authKeyOrResourceToken
);

#region database
// Database reference with creation if it does not already exist
Microsoft.Azure.Cosmos.Database database = await client.CreateDatabaseIfNotExistsAsync(
    id: "adventureworks"
);

Console.WriteLine($"New database:\t{database.Id}");
#endregion database


#region container
// Container reference with creation if it does not alredy exist
Container container = await database.CreateContainerIfNotExistsAsync(
    id: "products",
    partitionKeyPath: "/category"
    // throughput: 400
);

Console.WriteLine($"New container:\t{container.Id}");
#endregion container


#region create item
// C# record representing an item in the container

var categories = new List<string> {"vestes et parkas", "tee-shirts", "survetements", "polos", "pantalons"}; 

var products = new Faker<Product>()
        .RuleFor(c => c.id, f => Guid.NewGuid().ToString())
        .RuleFor(c => c.category, f => f.PickRandom<string>(categories))
        .RuleFor(c => c.category, f => f.Company.CompanyName())
        .RuleFor(c => c.sale, f => f.PickRandomParam(new bool[] {true, true, false}))
        .RuleFor(c => c.quantity, f => f.Random.Int(0, 500)).Generate(100).ToList();


// Create new object and upsert (create or replace) to container
foreach(var product in products) 
{
    // Product newItem = new(
    //     id: "68719518391",
    //     category: "gear-surf-surfboards",
    //     name: "Yamba Surfboard",
    //     quantity: 12,
    //     sale: false
    // );
    Product createdItem = await container.UpsertItemAsync<Product>(
        item: product,
        partitionKey: new PartitionKey(product.category)
    );

    Console.WriteLine($"Created item:\t{createdItem.id}\t[{createdItem.category}]");
}




#endregion create item

#region Get Item
// Point read item from container using the id and partitionKey
Product readItem = await container.ReadItemAsync<Product>(
    id: "68719518391",
    partitionKey: new PartitionKey("gear-surf-surfboards")
);

Console.WriteLine($"Get item:\t{readItem.id}\t[{readItem.category}]\t[{readItem.name}]");
#endregion Get Item

#endregion Application code should start here.

await host.RunAsync();