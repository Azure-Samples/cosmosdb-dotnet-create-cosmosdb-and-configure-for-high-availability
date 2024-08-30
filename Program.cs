// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Microsoft.Azure.Cosmos;

namespace HACosmosDB
{
    public class Program
    {
        private const int MAX_STALENESS_PREFIX = 300;
        private const int MAX_INTERVAL_IN_SECONDS = 1000;

        private static ResourceIdentifier? _resourceGroupId = null;
        private static String DATABASE_ID = "TestDB";
        private static String CONTAINER_ID = "TestCollection";
        private static AzureLocation TARGET_LOCATION = AzureLocation.WestUS3;
        private static String IP_Address = "23.43.230.120";

        /**
         * Azure CosmosDB sample -
         *  - Create a CosmosDB configured with a single read location
         *  - Get the credentials for the CosmosDB
         *  - Update the CosmosDB with additional read locations
         *  - Delete the CosmosDB
         */
        public static async Task RunSample(ArmClient client)
        {
            try
            {
                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                // Create a resource group in the EastUS region
                string resourceGroupName = Utilities.CreateRandomName("CosmosDBTemplateRG");
                Utilities.Log($"Creating resource group '{resourceGroupName}'...");
                ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, new ResourceGroupData(TARGET_LOCATION));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log($"Resource group '{resourceGroup.Data.Name}' is created.");

                //============================================================
                // Create a CosmosDB.

                string dbAccountName = Utilities.CreateRandomName("dbaccount");
                Utilities.Log($"Creating CosmosDB account '{dbAccountName}'...");
                var locations = new List<CosmosDBAccountLocation>()
                {
                    new CosmosDBAccountLocation(){ LocationName  = TARGET_LOCATION, FailoverPriority = 0 },
                };
                var dbAccountInput = new CosmosDBAccountCreateOrUpdateContent(TARGET_LOCATION, locations)
                {
                    Kind = CosmosDBAccountKind.MongoDB,
                    ConsistencyPolicy = new ConsistencyPolicy(DefaultConsistencyLevel.BoundedStaleness)
                    {
                        MaxStalenessPrefix = MAX_STALENESS_PREFIX,
                        MaxIntervalInSeconds = MAX_INTERVAL_IN_SECONDS
                    },
                    IPRules =
                    {
                        new CosmosDBIPAddressOrRange()
                        {
                            IPAddressOrRange = IP_Address
                        }
                    },
                    IsVirtualNetworkFilterEnabled = true,
                    EnableAutomaticFailover = false,
                    ConnectorOffer = ConnectorOffer.Small,
                    DisableKeyBasedMetadataWriteAccess = false,
                };

                dbAccountInput.Tags.Add("key1", "foo");
                dbAccountInput.Tags.Add("key2", "bar");
                var accountLro = await resourceGroup.GetCosmosDBAccounts().CreateOrUpdateAsync(WaitUntil.Completed, dbAccountName, dbAccountInput);
                CosmosDBAccountResource dbAccount = accountLro.Value;
                Utilities.Log($"CosmosDB account '{dbAccount.Id.Name}' is created.");

                //============================================================
                // Get credentials for the CosmosDB.

                Utilities.Log($"Retrieving credentials of CosmosDB account '{dbAccount.Id.Name}'...");
                var getKeysLro = await dbAccount.GetKeysAsync();
                CosmosDBAccountKeyList keyList = getKeysLro.Value;
                string masterKey = keyList.PrimaryMasterKey;
                string endPoint = dbAccount.Data.DocumentEndpoint;
                Utilities.Log($"Master Key: {masterKey}");
                Utilities.Log($"Endpoint: {endPoint}");

                //============================================================
                // Update document db with three additional read regions
                Utilities.Log($"Updating CosmosDB account '{dbAccount.Id.Name}' with three additional read replication regions...");
                var updataInput = new CosmosDBAccountPatch()
                {
                    Locations =
                    {
                        new CosmosDBAccountLocation() { LocationName = TARGET_LOCATION, FailoverPriority = 0 },
                        new CosmosDBAccountLocation() { LocationName = AzureLocation.EastAsia, FailoverPriority = 1 },
                        new CosmosDBAccountLocation() { LocationName = AzureLocation.UKSouth, FailoverPriority = 2 },
                        new CosmosDBAccountLocation() { LocationName = AzureLocation.SouthAfricaNorth, FailoverPriority = 3 }
                    }
                };
                var updateResponse = await dbAccount.UpdateAsync(WaitUntil.Completed, updataInput);
                CosmosDBAccountResource updatedDBAccount = updateResponse.Value;
                Utilities.Log($"CosmosDB account '{dbAccount.Id.Name}' is updated.");

                //============================================================
                // Connect to CosmosDB and add a container

                await CreateDBAndAddCollection(masterKey, endPoint);

                //============================================================
                // Delete CosmosDB
                Utilities.Log($"Deleting CosmosDB account '{dbAccount.Id.Name}'...");
                await dbAccount.DeleteAsync(WaitUntil.Completed);
                Utilities.Log($"CosmosDB account '{dbAccount.Id.Name}' is deleted.");
            }
            catch (Exception ex)
            {
                Utilities.Log(ex.Message);
                Utilities.Log(ex.StackTrace);
            }
            finally
            {
                if (_resourceGroupId is not null)
                {
                    Utilities.Log($"Deleting resource group '{_resourceGroupId.Name}'...");
                    try
                    {
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Resource group '{_resourceGroupId.Name}' is deleted.");
                    }
                    catch (Exception e)
                    {
                        Utilities.Log(e.Message);
                        Utilities.Log(e.StackTrace);
                    }
                }
            }
        }

        private static async Task CreateDBAndAddCollection(string masterKey, string endpoint)
        {
            Utilities.Log("Connecting and adding container");

            CosmosClient cosmosClient = new CosmosClient(endpoint,
                    masterKey, new CosmosClientOptions());

            Database database = await cosmosClient.CreateDatabaseAsync(DATABASE_ID);

            Utilities.Log($"Database '{database.ToString()}' is created.");

            Container container = await database.CreateContainerAsync(CONTAINER_ID, "/partitionKeyPath", 4000);
        }

        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                AzureCliCredential credential = new AzureCliCredential();
                ArmClient client = new ArmClient(credential, subscriptionId);

                DATABASE_ID = Environment.GetEnvironmentVariable("DATABASE_ID") ?? DATABASE_ID;
                CONTAINER_ID = Environment.GetEnvironmentVariable("CONTAINER_ID") ?? CONTAINER_ID;
                TARGET_LOCATION = Environment.GetEnvironmentVariable("TARGET_LOCATION") ?? TARGET_LOCATION;
                IP_Address = Environment.GetEnvironmentVariable("IP_Address") ?? IP_Address;

                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e.Message);
                Utilities.Log(e.StackTrace);
            }
        }
    }
}
