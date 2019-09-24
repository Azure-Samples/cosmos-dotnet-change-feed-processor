---
languages:
- csharp
products:
- azure
- azure-cosmos-db
page_type: sample
description: "This sample shows you how to use the Azure Cosmos DB SDK to react to changes happening in the Azure Cosmos DB service."
---


# Consuming the Azure Cosmos DB Change Feed with Change Feed Processor
This sample shows you how yo use the [Azure Cosmos DB SDK](https://github.com/Azure/azure-cosmos-dotnet-v3) to consume Azure Cosmos DB's [Change Feed](https://docs.microsoft.com/azure/cosmos-db/change-feed) and react to changes happening in a container.

## Requirements

- An active Azure Cosmos account or the [Azure Cosmos DB Emulator](https://docs.microsoft.com/azure/cosmos-db/local-emulator) - If you don't have an account, refer to the [Create a database account](https://docs.microsoft.com/azure/cosmos-db/create-sql-api-dotnet#create-an-azure-cosmos-db-account) article.
- [NET Core SDK](https://dotnet.microsoft.com/download) or Visual Studio 2017 (or higher)

## Description

This sample will create two containers, using the names defined in [appSettings.json](./src/appSettings.json) (`SourceContainerName`, `LeasesContainerName`). One of them will be used to store state (`LeasesContainerName`), and the other will be the container that we'll be listening for changes on (`SourceContainerName`).

The code creates an instance of a Change Feed Processor and defines a delegate that will process the changes:

    ChangeFeedProcessor changeFeedProcessor = cosmosClient.GetContainer(databaseName, sourceContainerName)
        .GetChangeFeedProcessorBuilder<ToDoItem>("<name-for-the-workflow>", HandleChangesAsync)
            .WithInstanceName("<name-for-the-host-instance>")
            .WithLeaseContainer(leaseContainer)
            .Build();

Where `HandleChangesAsync` is a delegate with a signature of:

    async Task HandleChangesAsync(IReadOnlyCollection<ToDoItem> changes, CancellationToken cancellationToken)

The Change Feed Processor works as a push model. Whenever there are new changes in `SourceContainerName`, the HandleChangesAsync delegate will be called and its code will be able to process the list of changes. As more changes keep happening, new invocations will occur.

## Running this sample

1. Clone this repository or download the zip file.
2. Retrieve the **Connection String** value from the Keys blade of your Azure Cosmos account in the Azure portal. For more information on obtaining the Connection String for your Azure Cosmos account refer to [View, copy, and regenerate access keys and passwords](https://docs.microsoft.com/azure/cosmos-db/secure-access-to-data#master-keys).
    * If you are working with the Azure Cosmos DB Emulator, refer to [Develop locally with the Azure Cosmos Emulator](https://docs.microsoft.com/azure/cosmos-db/local-emulator#authenticating-requests).
3. In the [appSettings.json](./src/appSettings.json) file, located in the project root, find **ConnectionString** and replace the placeholder value with the value obtained for your account.
4. Run `dotnet run` or press **F5** from within Visual Studio.
5. Type an amount of random records to be generated in `SourceContainerName` (for example, 10), and press ENTER. The sample will write those records in the container.
6. The `HandleChangesAsync` delegate will get called and handle those changes as they are inserted, independently.
7. Repeat step 5 with different numbers or type `exit` to stop.

## About the code
The code included in this sample is intended to get you going with consuming the Change Feed and react to changes. It is not intended to be a set of best practices on how to build scalable enterprise grade applications. This is beyond the scope of this quick start sample. 

## More information

- [Azure Cosmos DB Documentation](https://docs.microsoft.com/azure/cosmos-db)
- [Azure Cosmos DB .NET SDK](https://docs.microsoft.com/azure/cosmos-db/sql-api-sdk-dotnet)
- [Azure Cosmos DB .NET SDK Reference Documentation](https://docs.microsoft.com/dotnet/api/overview/azure/cosmosdb?view=azure-dotnet)
