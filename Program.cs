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
                string rgName = Utilities.CreateRandomName("CosmosDBTemplateRG");
                Utilities.Log($"creating resource group with name:{rgName}");
                ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);

                //============================================================
                // Create a CosmosDB.

                Utilities.Log("Creating a CosmosDB...");
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
                var accountLro = await resourceGroup.GetCosmosDBAccounts().CreateOrUpdateAsync(WaitUntil.Completed, dbAccountName, dbAccountInput);
                CosmosDBAccountResource dbAccount = accountLro.Value;
                Utilities.Log("Created CosmosDB");

                //============================================================
                // Get credentials for the CosmosDB.

                Utilities.Log("Get credentials for the CosmosDB");
                var getKeysLro = await dbAccount.GetKeysAsync();
                CosmosDBAccountKeyList keyList = getKeysLro.Value;
                string masterKey = keyList.PrimaryMasterKey;
                Utilities.Log($"masterKey: {masterKey}");

                //============================================================
                // Update document db with three additional read regions
                Utilities.Log("Updating CosmosDB with three additional read replication regions");
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
                var updateResponse = await dbAccount.UpdateAsync(WaitUntil.Completed, updataInput);
                CosmosDBAccountResource updatedDBAccount = updateResponse.Value;
                Utilities.Log("Updated CosmosDB");

                //============================================================
                // Delete CosmosDB
                Utilities.Log("Deleting the CosmosDB");
                try
                {
                    await dbAccount.DeleteAsync(WaitUntil.Completed);
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex.ToString());
                }
                Utilities.Log("Deleted the CosmosDB");
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group: {_resourceGroupId}");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId}");
                    }
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception e)
                {
                    Utilities.Log(e.StackTrace);
                }
            }
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
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

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
