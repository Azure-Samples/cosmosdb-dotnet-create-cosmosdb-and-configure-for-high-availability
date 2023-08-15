// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using System.Reflection;

namespace HACosmosDB
{
    public class Program
    {
        private const int _maxStalenessPrefix = 300;
        private const int _maxIntervalInSeconds = 1000;
        const String DATABASE_ID = "TestDB";
        const String COLLECTION_ID = "TestCollection";

        /**
         * Azure CosmosDB sample -
         *  - Create a CosmosDB configured with a single read location
         *  - Get the credentials for the CosmosDB
         *  - Update the CosmosDB with additional read locations
         *  - add collection to the CosmosDB with throughput 4000
         *  - Delete the CosmosDB
         */
        public static async Task RunSample(ArmClient client)
        {
            // Get default subscription
            SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

            // Create a resource group in the EastUS region
            string rgName = Utilities.CreateRandomName("CosmosDBTemplateRG");
            Utilities.Log($"created resource group with name:{rgName}");
            //ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
            //ResourceGroupResource resourceGroup = rgLro.Value;
            //Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);

            var rg = await subscription.GetResourceGroups().GetAsync("dbaccount-8657");

            await foreach (var item in rg.Value.GetCosmosDBAccounts().GetAllAsync())
            {
                await Console.Out.WriteLineAsync(item.Id);
                await Console.Out.WriteLineAsync(item.Data.Kind.ToString());
            }
            Utilities.Log();
            Utilities.Log();
            var dbAccountCollection = rg.Value.GetCosmosDBAccounts();
            string dbAccountName = Utilities.CreateRandomName("dbaccount");

            //var dbAccount = await CreateDatabaseAccount(rg, CosmosDBAccountKind.GlobalDocumentDB, dbAccountName);
            //Utilities.Log(dbAccount.Data.Id);
            //try
            //{
            //    //============================================================
            //    // Create a CosmosDB.
            //    string mongoDBName = Utilities.CreateRandomName("mongoDB");
            //    var mongoDBDatabaseCreateUpdateOptions = new MongoDBCollectionCreateOrUpdateContent(AzureLocation.WestUS, new MongoDBCollectionResourceInfo(mongoDBName))
            //    {
            //        Options = new CosmosDBCreateUpdateConfig
            //        {
            //            Throughput = 700,
            //        }
            //    };
            //    resourceGroup.GetCosmosDBAccounts()
            //    var mongoDBContainerLro = await mongoDBContainerCollection.CreateOrUpdateAsync(WaitUntil.Completed, mongoDBName, mongoDBDatabaseCreateUpdateOptions);


            //    Console.WriteLine("Creating a CosmosDB...");
            //    ICosmosDBAccount cosmosDBAccount = azure.CosmosDBAccounts.Define(docDBName)
            //            .WithRegion(Region.USWest)
            //            .WithNewResourceGroup(rgName)
            //            .WithKind(DatabaseAccountKind.GlobalDocumentDB)
            //            .WithSessionConsistency()
            //            .WithWriteReplication(Region.USEast)
            //            .WithReadReplication(Region.USCentral)
            //            .WithIpRangeFilter("13.91.6.132,13.91.6.1/24")
            //            .Create();

            //    Console.WriteLine("Created CosmosDB");
            //    Utilities.Print(cosmosDBAccount);

            //    //============================================================
            //    // Update document db with three additional read regions

            //    Console.WriteLine("Updating CosmosDB with three additional read replication regions");
            //    cosmosDBAccount = cosmosDBAccount.Update()
            //            .WithReadReplication(Region.AsiaEast)
            //            .WithReadReplication(Region.AsiaSouthEast)
            //            .WithReadReplication(Region.UKSouth)
            //            .Apply();

            //    Console.WriteLine("Updated CosmosDB");
            //    Utilities.Print(cosmosDBAccount);

            //    //============================================================
            //    // Get credentials for the CosmosDB.

            //    Console.WriteLine("Get credentials for the CosmosDB");
            //    var databaseAccountListKeysResult = cosmosDBAccount.ListKeys();
            //    string masterKey = databaseAccountListKeysResult.PrimaryMasterKey;
            //    string endPoint = cosmosDBAccount.DocumentEndpoint;

            //    //============================================================
            //    // Connect to CosmosDB and add a collection

            //    Console.WriteLine("Connecting and adding collection");
            //    //CreateDBAndAddCollection(masterKey, endPoint);

            //    //============================================================
            //    // Delete CosmosDB
            //    Console.WriteLine("Deleting the CosmosDB");
            //    // work around CosmosDB service issue returning 404 CloudException on delete operation
            //    try
            //    {
            //        azure.CosmosDBAccounts.DeleteById(cosmosDBAccount.Id);
            //    }
            //    catch (CloudException)
            //    {
            //    }
            //    Console.WriteLine("Deleted the CosmosDB");
            //}
            //finally
            //{
            //    try
            //    {
            //        Utilities.Log("Deleting resource group: " + rgName);
            //        azure.ResourceGroups.BeginDeleteByName(rgName);
            //        Utilities.Log("Deleted resource group: " + rgName);
            //    }
            //    catch (NullReferenceException)
            //    {
            //        Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
            //    }
            //    catch (Exception e)
            //    {
            //        Utilities.Log(e.StackTrace);
            //    }
            //}
        }

        protected static async Task<CosmosDBAccountResource> CreateDatabaseAccount(ResourceGroupResource resourceGroup, CosmosDBAccountKind kind, string dbAccountName)
        {
            var locations = new List<CosmosDBAccountLocation>()
            {
                new CosmosDBAccountLocation(){ LocationName  = AzureLocation.NorthEurope},
            };
            var dbAccountInput = new CosmosDBAccountCreateOrUpdateContent(AzureLocation.WestUS2, locations)
            {
                Kind = kind,
                ConsistencyPolicy = new ConsistencyPolicy(DefaultConsistencyLevel.BoundedStaleness)
                {
                    MaxStalenessPrefix = _maxStalenessPrefix,
                    MaxIntervalInSeconds = _maxIntervalInSeconds
                },
                IPRules =
                    {
                        new CosmosDBIPAddressOrRange()
                        {
                            IPAddressOrRange = "23.43.235.120"
                        }
                    },
                IsVirtualNetworkFilterEnabled = true,
                EnableAutomaticFailover = false,
                ConnectorOffer = ConnectorOffer.Small,
                DisableKeyBasedMetadataWriteAccess = false,
            };

            dbAccountInput.Tags.Add("key1", "value");
            dbAccountInput.Tags.Add("key2", "value");
            var accountLro = await resourceGroup.GetCosmosDBAccounts().CreateOrUpdateAsync(WaitUntil.Completed, dbAccountName, dbAccountInput);
            return accountLro.Value;
        }

        public static async Task Main(string[] args)
        {
            //=================================================================
            // Authenticate
            var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
            var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
            ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            ArmClient client = new ArmClient(credential, subscription);

            await RunSample(client);
            try
            {

            }
            catch (Exception e)
            {
                Utilities.Log(e.Message);
                Utilities.Log(e.StackTrace);
            }
        }
    }
}
