using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.ResourceActions;

namespace Track1Perf
{
    public class Program
    {
        public static string rgName = "sdk-perf-test-rg";
        static async Task Main(string[] args)
        {
            string clienId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            string tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

            var servicePrincipalLoginInformation = new ServicePrincipalLoginInformation 
            { 
                ClientId = clienId,
                ClientSecret = clientSecret
            };
            AzureCredentials credentials = new AzureCredentials(servicePrincipalLoginInformation, tenantId, AzureEnvironment.AzureGlobalCloud);

            var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

            var resourceGroup = azure.ResourceGroups.Define(rgName)
                    .WithRegion(Region.USEast2)
                    .Create();

            var t1 = DateTimeOffset.Now.UtcDateTime;
            Console.WriteLine("Creating the KeyVaults");

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                string vaultName = "sdk-perf-vault-one" + i;
                Task task = CreateVault(azure, vaultName);
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
                Task task = ListVault(azure);
                TaskList2.Add(task);
            }
            await Task.WhenAll(TaskList2.ToArray());
            t2 = DateTimeOffset.Now.UtcDateTime;

            Console.WriteLine($"KeyVaults list: took {(t2 - t1).TotalMilliseconds } milliseconds to list 10 KeyVaults 100 times !!");

            // Delete ResourceGroup
            Console.WriteLine("Deleting Resource Group: " + rgName);

            azure.ResourceGroups.DeleteByName(rgName);

            Console.WriteLine("Deleted Resource Group: " + rgName);
        }

        public static async Task<IVault> CreateVault(IAzure azure, string vaultName)
        {
            var vault = await azure.Vaults
                    .Define(vaultName)
                    .WithRegion(Region.USEast2)
                    .WithNewResourceGroup(rgName)
                    .WithEmptyAccessPolicy().CreateAsync();
            Console.WriteLine("KeyVault created for: " + vault.Id);
            return vault;
        }

        public static async Task<IPagedCollection<IVault>> ListVault(IAzure azure)
        {
            var vaults = await azure.Vaults.ListByResourceGroupAsync(rgName);
            return vaults;
        }
    }
}
