using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.CollectionActions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure203Console
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && "create".Equals(args[0]))
            {
                new Program().CreateObjects(null);
            }
            else if (args.Length > 0 && "delete".Equals(args[0]))
            {
                new Program().DeleteResourceGroup(null);
            }
        }

        IAzure ConfigureAzure()
        {
            Console.WriteLine("AZURE_AUTH_LOCATION: " + Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));
            var credentials = SdkContext.AzureCredentialsFactory
                .FromFile(Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));
            
            return Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();
        }

        T FindInCollectionOrNull<T>(ISupportsListing<T> collection, string entityTypeDisplayName, string name)
            where T: IResource
        {
            Console.WriteLine("Searching for " + entityTypeDisplayName + "...");
            T entity = default(T);
            foreach (T item in collection.List().ToList())
            {
                if (name.Equals(item.Name))
                {
                    entity = item;
                    break;
                }
            }
            return entity;
        }

        IResourceGroup GetOrCreateResourceGroup(IAzure azure, string name, Region location)
        {
            IResourceGroup resourceGroup = FindInCollectionOrNull(azure.ResourceGroups, "a resource group", name);
            if (resourceGroup == null)
            {
               Console.WriteLine("Creating a resource group...");
               resourceGroup = azure.ResourceGroups.Define(name)
                   .WithRegion(location)
                   .Create();
            }
            return resourceGroup;
        }

        IAvailabilitySet GetOrCreateAvailabilitySet(IAzure azure, string name, Region location, string groupName)
        {
            IAvailabilitySet availabilitySet = FindInCollectionOrNull(azure.AvailabilitySets, "an availability set", name);
            if (availabilitySet == null)
            {
                Console.WriteLine("Creating an availability set...");
                availabilitySet = azure.AvailabilitySets.Define(name)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithSku(AvailabilitySetSkuTypes.Aligned)
                    .Create();
            }
            return availabilitySet;
        }

        IPublicIPAddress GetOrCreatePublicIPAddress(IAzure azure, string name, Region location, string groupName)
        {
            IPublicIPAddress publicIPAddress = FindInCollectionOrNull(azure.PublicIPAddresses, "a public IP address", name);
            if (publicIPAddress == null)
            {
                Console.WriteLine("Creating a public IP address...");
                publicIPAddress = azure.PublicIPAddresses.Define(name)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithDynamicIP()
                    .Create();
            }
            return publicIPAddress;
        }

        INetwork GetOrCreateNetwork(IAzure azure, string name, Region location, string groupName)
        {
            INetwork network = FindInCollectionOrNull(azure.Networks, "a virtual network", name);
            if (network == null)
            {
                Console.WriteLine("Creating a virtual network...");
                network = azure.Networks.Define(name)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithAddressSpace("10.0.0.0/16")
                    .WithSubnet("mySubnet", "10.0.0.0/24")
                    .Create();
            }
            return network;
        }

        INetworkInterface GetOrCreateNetworkInterface(IAzure azure, string name, Region location, string groupName,
            IPublicIPAddress publicIPAddress, INetwork network)
        {
            INetworkInterface networkInterface = FindInCollectionOrNull(azure.NetworkInterfaces, "a network interface", name);
            if (networkInterface == null)
            {
                Console.WriteLine("Creating a network interface...");
                networkInterface = azure.NetworkInterfaces.Define(name)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetwork(network)
                    .WithSubnet("mySubnet")
                    .WithPrimaryPrivateIPAddressDynamic()
                    .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                    .Create();
            }
            return networkInterface;
        }

        IVirtualMachine GetOrCreateVirtualMachine(IAzure azure, string name, Region location, string groupName,
            INetworkInterface networkInterface, IAvailabilitySet availabilitySet)
        {
            IVirtualMachine virtualMachine = FindInCollectionOrNull(azure.VirtualMachines, "a virtual machine", name);
            if (virtualMachine == null)
            {
                Console.WriteLine("Creating a virtual machine...");
                virtualMachine = azure.VirtualMachines.Define(name)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer", "2019-Datacenter")
                    .WithAdminUsername("azureuser")
                    .WithAdminPassword("Azure12345678")
                    .WithComputerName(name)
                    .WithExistingAvailabilitySet(availabilitySet)
                    .WithSize(VirtualMachineSizeTypes.StandardDS1V2)
                    .Create();
            }
            return virtualMachine;
        }

        void CreateObjects(IAzure azure)
        {
            if (azure == null)
            {
                azure = ConfigureAzure();
            }
            var groupName = "azure203ResourceGroup";
            var location = Region.USEast;
            var resourceGroup = GetOrCreateResourceGroup(azure, groupName, location);
            var availabilitySet = GetOrCreateAvailabilitySet(azure, "myAvSet", location, groupName);
            var publicIPAddress = GetOrCreatePublicIPAddress(azure, "myPublicIP", location, groupName);
            var network = GetOrCreateNetwork(azure, "myVNet", location, groupName);
            var networkInterface = GetOrCreateNetworkInterface(azure, "myNIC", location, groupName,
                publicIPAddress, network);
            var virtualMachine = GetOrCreateVirtualMachine(azure, "myVM", location, groupName,
                networkInterface, availabilitySet);
            Console.WriteLine("Stopping a virtual machine...");
            virtualMachine.PowerOff();
            Console.WriteLine("A virtual machine has stopped");
        }

        void DeleteResourceGroup(IAzure azure)
        {
            if (azure == null)
            {
                azure = ConfigureAzure();
            }
            var groupName = "azure203ResourceGroup";
            Console.WriteLine("Deleting Resource Group " + groupName + " ...");
            azure.ResourceGroups.DeleteByName(groupName);
        }
    }
}
