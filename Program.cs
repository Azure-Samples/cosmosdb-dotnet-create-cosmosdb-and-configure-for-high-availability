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
        private static ResourceIdentifier? _resourceGroupId = null; 
        private const int _maxStalenessPrefix = 100000;
        private const int _maxIntervalInSeconds = 300;

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
            //try
            {
                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                // Create a resource group in the EastUS region
                string rgName = Utilities.CreateRandomName("CosmosDBTemplateRG");
                rgName = "CosmosDBTemplateRG0000";
                Utilities.Log($"creating resource group with name:{rgName}");
                ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);

                //============================================================
                // Create a CosmosDB.

                Console.WriteLine("Creating a CosmosDB...");
                string dbAccountName = Utilities.CreateRandomName("dbaccount");
                CosmosDBAccountKind cosmosDBKind = CosmosDBAccountKind.GlobalDocumentDB;
                var locations = new List<CosmosDBAccountLocation>()
                {
                    new CosmosDBAccountLocation(){ LocationName  = AzureLocation.EastUS, FailoverPriority = 0 },
                };
                var dbAccountInput = new CosmosDBAccountCreateOrUpdateContent(AzureLocation.WestUS2, locations)
                {
                    Kind = cosmosDBKind,
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
                    EnableMultipleWriteLocations = true,
                };

                dbAccountInput.Tags.Add("key1", "value");
                dbAccountInput.Tags.Add("key2", "value");
                //var accountLro = await resourceGroup.GetCosmosDBAccounts().CreateOrUpdateAsync(WaitUntil.Completed, dbAccountName, dbAccountInput);
                var accountLro = await resourceGroup.GetCosmosDBAccounts().GetAsync("dbaccount6183");
                CosmosDBAccountResource dbAccount = accountLro.Value;

                Console.WriteLine(dbAccount.Data.FailoverPolicies.Count);
                foreach (var item in dbAccount.Data.FailoverPolicies)
                {
                    await Console.Out.WriteLineAsync(item.LocationName + "-" + item.FailoverPriority);
                }
                Console.WriteLine("Created CosmosDB");

                //============================================================
                // Update document db with three additional read regions
                Console.WriteLine("Updating CosmosDB with three additional read replication regions");
                var updataInput = new CosmosDBAccountPatch()
                {
                    Locations =
                    {
                        new CosmosDBAccountLocation() { LocationName = AzureLocation.EastUS, FailoverPriority = 0 },
                        new CosmosDBAccountLocation() { LocationName = AzureLocation.EastAsia, FailoverPriority = 1 },
                        new CosmosDBAccountLocation() { LocationName = AzureLocation.UKSouth, FailoverPriority = 2 },
                        new CosmosDBAccountLocation() { LocationName = AzureLocation.SouthAfricaNorth, FailoverPriority = 3 }
                    }
                };
                //var updateResponse  = await dbAccount.UpdateAsync(WaitUntil.Completed, updataInput);
                //CosmosDBAccountResource updatedDBAccount = updateResponse.Value;
                Utilities.Log("Updated CosmosDB");

                //============================================================
                // Get credentials for the CosmosDB.

                Console.WriteLine("Get credentials for the CosmosDB");
                var getKeysLro = await dbAccount.GetKeysAsync();
                CosmosDBAccountKeyList keyList = getKeysLro.Value;
                string masterKey = keyList.PrimaryMasterKey;
                //string endPoint = cosmosDBAccount.DocumentEndpoint;

                //============================================================
                // Connect to CosmosDB and add a collection

                Console.WriteLine("Connecting and adding collection");
                //CreateDBAndAddCollection(masterKey, endPoint);

                ////============================================================
                //// Delete CosmosDB
                //Console.WriteLine("Deleting the CosmosDB");
                //work around CosmosDB service issue returning 404 CloudException on delete operation
                //try
                //{
                //    azure.CosmosDBAccounts.DeleteById(cosmosDBAccount.Id);
                //}
                //catch (CloudException)
                //{
                //}
                //Console.WriteLine("Deleted the CosmosDB");
            }
            //finally
            //{
            //    try
            //    {
            //        if (_resourceGroupId is not null)
            //        {
            //            Console.WriteLine($"Deleting Resource Group: {_resourceGroupId}");
            //            await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
            //            Console.WriteLine($"Deleted Resource Group: {_resourceGroupId}");
            //        }
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
                new CosmosDBAccountLocation(){ LocationName  = AzureLocation.NorthEurope, FailoverPriority = 0 },
                new CosmosDBAccountLocation(){ LocationName  = AzureLocation.SoutheastAsia, FailoverPriority = 1 },
                new CosmosDBAccountLocation(){ LocationName  = AzureLocation.UKSouth, FailoverPriority = 2 },
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
                EnableMultipleWriteLocations = true,
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
