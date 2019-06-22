using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace ChangeFeedSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration configuration = BuildConfiguration();

            CosmosClient cosmosClient = BuildCosmosClient(configuration);

            await InitializeContainersAsync(cosmosClient, configuration);

            ChangeFeedProcessor processor = await StartChangeFeedProcessorAsync(cosmosClient, configuration);

            await GenerateItemsAsync(cosmosClient, processor, configuration);
        }

        /// <summary>
        /// The delegate receives batches of changes as they are generated in the change feed and can process them.
        /// </summary>
        static async Task HandleChangesAsync(IReadOnlyCollection<ToDoItem> changes, CancellationToken cancellationToken)
        {
            Console.WriteLine("Started handling changes...");
            foreach (ToDoItem item in changes)
            {
                Console.WriteLine($"Detected operation for item with id {item.id}, created at {item.creationTime}.");
                // Simulate some asynchronous operation
                await Task.Delay(10);
            }

            Console.WriteLine("Finished handling changes.");
        }

        /// <summary>
        /// Create required containers for the sample.
        /// Change Feed processing requires a source container to read the Change Feed from, and a container to store the state on, called leases.
        /// </summary>
        private static async Task InitializeContainersAsync(
            CosmosClient cosmosClient,
            IConfiguration configuration)
        {
            string databaseName = configuration["SourceDatabaseName"];
            string sourceContainerName = configuration["SourceContainerName"];
            string leaseContainerName = configuration["LeasesContainerName"];

            if (string.IsNullOrEmpty(databaseName)
                || string.IsNullOrEmpty(sourceContainerName)
                || string.IsNullOrEmpty(leaseContainerName))
            {
                throw new ArgumentNullException("'SourceDatabaseName', 'SourceContainerName', and 'LeasesContainerName' settings are required. Verify your configuration.");
            }

            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);

            await database.CreateContainerIfNotExistsAsync(new ContainerProperties(sourceContainerName, "/id"));

            await database.CreateContainerIfNotExistsAsync(new ContainerProperties(leaseContainerName, "/id"));
        }

        /// <summary>
        /// Start the Change Feed Processor to listen for changes and process them with the HandlerChangesAsync implementation.
        /// </summary>
        private static async Task<ChangeFeedProcessor> StartChangeFeedProcessorAsync(
            CosmosClient cosmosClient,
            IConfiguration configuration)
        {
            string databaseName = configuration["SourceDatabaseName"];
            string sourceContainerName = configuration["SourceContainerName"];
            string leaseContainerName = configuration["LeasesContainerName"];

            Container leaseContainer = cosmosClient.GetContainer(databaseName, leaseContainerName);
            ChangeFeedProcessor changeFeedProcessor = cosmosClient.GetContainer(databaseName, sourceContainerName)
                .GetChangeFeedProcessorBuilder<ToDoItem>("changeFeedSample", HandleChangesAsync)
                    .WithInstanceName("consoleHost")
                    .WithLeaseContainer(leaseContainer)
                    .Build();

            Console.WriteLine("Starting Change Feed Processor...");
            await changeFeedProcessor.StartAsync();
            Console.WriteLine("Change Feed Processor started.");
            return changeFeedProcessor;
        }

        /// <summary>
        /// Generate sample items based on user input.
        /// </summary>
        private static async Task GenerateItemsAsync(
            CosmosClient cosmosClient,
            ChangeFeedProcessor changeFeedProcessor,
            IConfiguration configuration)
        {
            string databaseName = configuration["SourceDatabaseName"];
            string sourceContainerName = configuration["SourceContainerName"];
            Container sourceContainer = cosmosClient.GetContainer(databaseName, sourceContainerName);
            while(true)
            {
                Console.WriteLine("Enter a number of items to insert in the container or 'exit' to stop:");
                string command = Console.ReadLine();
                if ("exit".Equals(command, StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine();
                    break;
                }

                if (int.TryParse(command, out int itemsToInsert))
                {
                    Console.WriteLine($"Generating {itemsToInsert} items...");
                    for (int i = 0; i < itemsToInsert; i++)
                    {
                        string id = Guid.NewGuid().ToString();
                        await sourceContainer.CreateItemAsync<ToDoItem>(
                            new ToDoItem()
                            {
                                id = id,
                                creationTime = DateTime.UtcNow
                            },
                            new PartitionKey(id));
                    }
                }
            }

            Console.WriteLine("Stopping Change Feed Processor...");
            await changeFeedProcessor.StopAsync();
            Console.WriteLine("Stopped Change Feed Processor.");
        }

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
        }

        private static CosmosClient BuildCosmosClient(IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration["ConnectionString"]) || "<Your-Connection-String>".Equals(configuration["ConnectionString"]))
            {
                throw new ArgumentNullException("Missing 'ConnectionString' setting in configuration.");
            }

            return new CosmosClientBuilder(configuration["ConnectionString"])
                .Build();
        }
    }
}
