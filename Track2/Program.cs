using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;

namespace Track2Perf
{
    public class Program
    {
        public static string rgName = "sdk-stress-test";

        static async Task Main(string[] args)
        {
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            Guid tenantId = new Guid(Environment.GetEnvironmentVariable("AZURE_TENANT_ID"));
            string objectId = Environment.GetEnvironmentVariable("AZURE_OBJECT_ID");
            string location = "eastus2";

            var credential = new DefaultAzureCredential();

            var resourceClient = new ResourcesManagementClient(subscriptionId, credential);
            var resourceGroups = resourceClient.ResourceGroups;
            var keyVaultManagementClient = new KeyVaultManagementClient(subscriptionId, credential);
            var vaults = keyVaultManagementClient.Vaults;

            // Create Resource Group
            Console.WriteLine("Creating Resource Group: " + rgName);

            var resourceGroup = new ResourceGroup(location);
            await resourceGroups.CreateOrUpdateAsync(rgName, resourceGroup);

            Console.WriteLine("Created Resource Group: " + rgName);

            // Prepare creatable KeyVaults
            var creatableKeyVaults = new Dictionary<string, VaultCreateOrUpdateParameters> { };

            for (int i = 1; i <= 10; i++)
            {
                string vaultName = "sdk-perf-vault" + i;
                var vaultProperties = new VaultProperties(tenantId, new Azure.ResourceManager.KeyVault.Models.Sku(SkuName.Standard))
                {
                    AccessPolicies = new[] { new AccessPolicyEntry(tenantId, objectId, new Permissions()) }
                };
                var vaultParameters = new VaultCreateOrUpdateParameters(location, vaultProperties);

                creatableKeyVaults.Add(vaultName, vaultParameters);
            }

            // Create KeyVaults
            var t1 = DateTimeOffset.Now.UtcDateTime;
            Console.WriteLine("Creating the KeyVaults");

            List<Task> TaskList = new List<Task>();
            foreach (var item in creatableKeyVaults)
            {
                Task task = CreateKeyVaults(vaults, item.Key, item.Value);
                TaskList.Add(task);
            }
            await Task.WhenAll(TaskList.ToArray());

            var t2 = DateTimeOffset.Now.UtcDateTime;

            Console.WriteLine("Created KeyVaults");
            Console.WriteLine($"KeyVaults create: took {(t2 - t1).TotalSeconds } seconds to create 10 KeyVaults !!");

            // List
            Console.WriteLine("Listing the KeyVaults 100 times");

            t1 = DateTimeOffset.Now.UtcDateTime;
            List<Task> TaskList2 = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                Task task = ListVault(vaults);
                TaskList2.Add(task);
            }
            await Task.WhenAll(TaskList2.ToArray());
            t2 = DateTimeOffset.Now.UtcDateTime;

            Console.WriteLine($"KeyVaults list: took {(t2 - t1).TotalMilliseconds } milliseconds to list 10 KeyVaults 100 times !!");

            // Delete ResourceGroup
            Console.WriteLine("Deleting Resource Group: " + rgName);

            await (await resourceGroups.StartDeleteAsync(rgName)).WaitForCompletionAsync();

            Console.WriteLine("Deleted Resource Group: " + rgName);
        }

        public static async Task<Vault> CreateKeyVaults(VaultsOperations vaults, string vaultName, VaultCreateOrUpdateParameters vaultCreateOrUpdateParameters)
        {
            var result = await (await vaults
                        .StartCreateOrUpdateAsync(rgName, vaultName, vaultCreateOrUpdateParameters)).WaitForCompletionAsync();
            Console.WriteLine("KeyVault created for: " + result.Value.Id);
            return result.Value;
        }

        public static async Task<List<Vault>> ListVault(VaultsOperations vaults) 
        {
            var result = await  vaults.ListByResourceGroupAsync(rgName).ToEnumerableAsync();
            //Console.WriteLine("List");
            return result;
        }
    }

}
