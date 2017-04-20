using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Rest;
using System.Threading.Tasks;

namespace CreateVMUsingCs.Controllers
{
    public class VMController : Controller
    {
        // GET: VM
        public ActionResult Index()
        {
            return View();
        }



        public async Task CreateVM( )
        {
            var groupName = "myResourceGroup";
            var subscriptionId = "subsciptionId";
            var storageName = "myStorageAccount";
            var ipName = "myPublicIP";
            var subnetName = "mySubnet";
            var vnetName = "myVnet";
            var nicName = "myNIC";
            var avSetName = "myAVSet";
            var vmName = "myVM";
            var location = "location";
            var adminName = "adminName";
            var adminPassword = "adminPassword";


            //var vm = new VirtualMachineModel(grpname,loc,NetwInt,subnetName,avSetName,vnetName,storageName,adminname,adminpass,vmname);
            var token = await GetAccessTokenAsync();
            var credential = new TokenCredentials(token.AccessToken);

            var rgResult = await CreateResourceGroupAsync(
                credential,
                 groupName, subscriptionId, location
                 );
            var stResult = await CreateStorageAccountAsync(
              credential,
              groupName,
               subscriptionId,
              location,
              storageName);
            var strogres = stResult.ProvisioningState;
            //Console.ReadLine();

            //Public IP Address
            var ipResult = await CreatePublicIPAddressAsync(
              credential,
              groupName,
              subscriptionId,
              location,
              ipName);
            //Console.WriteLine(ipResult.Result.ProvisioningState);
            //var ipadress = ipResult.IpAddress;
            //Console.ReadLine();

            //Virtual Network
            var vnResult = await CreateVirtualNetworkAsync(
              credential,
              groupName,
              subscriptionId,
              location,
              vnetName,
              subnetName);
            var vnrs = vnResult.ProvisioningState;
            //Console.WriteLine(vnResult.Result.ProvisioningState);
            //Console.ReadLine();

            //Netwok Interface
            var ncResult = await CreateNetworkInterfaceAsync(
              credential,
              groupName,
              subscriptionId,
              location,
              subnetName,
              vnetName,
              ipName,
              nicName);
            var rest = ncResult.ProvisioningState;


            //AvailabilitySet
            var avResult = await CreateAvailabilitySetAsync(
              credential,
              groupName,
               subscriptionId,
              location,
              avSetName);


            //Virtual Machine
            var vmResult = await CreateVirtualMachineAsync(
              credential,
              groupName,
               subscriptionId,
              location,
              nicName,
              avSetName,
              storageName,
              adminName,
              adminPassword,
              vmName);

        }

        public static async Task<AuthenticationResult> GetAccessTokenAsync()
        {

            var cc = new ClientCredential("{client-id}", "{client-secret}");
            var context = new AuthenticationContext("https://login.windows.net/{tenant-id}");
             
            var token = await context.AcquireTokenAsync("https://management.azure.com/", cc);

            if (token == null)
            {
                throw new InvalidOperationException("Could not get the token");
            }
            return token;
        }
        public static async Task<ResourceGroup> CreateResourceGroupAsync(
          TokenCredentials credential,
          string groupName,
          string subscriptionId,
          string location)
        {
            var resourceManagementClient = new ResourceManagementClient(credential)
            { SubscriptionId = subscriptionId };

            //Console.WriteLine("Registering the providers...");
            var rpResult = resourceManagementClient.Providers.Register("Microsoft.Storage");
            // Console.WriteLine(rpResult.RegistrationState);
            rpResult = resourceManagementClient.Providers.Register("Microsoft.Network");
            // Console.WriteLine(rpResult.RegistrationState);
            rpResult = resourceManagementClient.Providers.Register("Microsoft.Compute");
            //Console.WriteLine(rpResult.RegistrationState);

            //Console.WriteLine("Creating the resource group...");
            var resourceGroup = new ResourceGroup { Location = location };
            return await resourceManagementClient.ResourceGroups.CreateOrUpdateAsync(groupName, resourceGroup);
        }
        public static async Task<StorageAccount> CreateStorageAccountAsync(
       TokenCredentials credential,
       string groupName,
       string subscriptionId,
       string location,
       string storageName)
        {
            // Console.WriteLine("Creating the storage account...");
            var storageManagementClient = new StorageManagementClient(credential)
            { SubscriptionId = subscriptionId };
            return await storageManagementClient.StorageAccounts.CreateAsync(
              groupName,
              storageName,
              new StorageAccountCreateParameters()
              {
                  Sku = new Microsoft.Azure.Management.Storage.Models.Sku()
                  { Name = SkuName.StandardLRS },
                  Kind = Kind.Storage,
                  Location = location
              }
            );
        }

        public static async Task<PublicIPAddress> CreatePublicIPAddressAsync(
           TokenCredentials credential,
           string groupName,
           string subscriptionId,
           string location,
           string ipName)
        {
            //Console.WriteLine("Creating the public ip...");
            var networkManagementClient = new NetworkManagementClient(credential)
            { SubscriptionId = subscriptionId };
            return await networkManagementClient.PublicIPAddresses.CreateOrUpdateAsync(
              groupName,
              ipName,
              new PublicIPAddress
              {
                  Location = location,
                  PublicIPAllocationMethod = "Dynamic"
              }
            );
        }
        public static async Task<VirtualNetwork> CreateVirtualNetworkAsync(
          TokenCredentials credential,
          string groupName,
          string subscriptionId,
          string location,
          string vnetName,
          string subnetName)
        {
            //Console.WriteLine("Creating the virtual network...");
            var networkManagementClient = new NetworkManagementClient(credential)
            { SubscriptionId = subscriptionId };

            var subnet = new Subnet
            {
                Name = subnetName,
                AddressPrefix = "10.0.0.0/24"
            };

            var address = new AddressSpace
            {
                AddressPrefixes = new List<string> { "10.0.0.0/16" }
            };

            return await networkManagementClient.VirtualNetworks.CreateOrUpdateAsync(
              groupName,
              vnetName,
              new VirtualNetwork
              {
                  Location = location,
                  AddressSpace = address,
                  Subnets = new List<Subnet> { subnet }
              }
            );
        }
        public static async Task<NetworkInterface> CreateNetworkInterfaceAsync(
          TokenCredentials credential,
          string groupName,
          string subscriptionId,
          string location,
          string subnetName,
          string vnetName,
          string ipName,
          string nicName)
        {
            var networkManagementClient = new NetworkManagementClient(credential)
            { SubscriptionId = subscriptionId };
            var subnet = await networkManagementClient.Subnets.GetAsync(
              groupName,
              vnetName,
              subnetName
            );
            var publicIP = await networkManagementClient.PublicIPAddresses.GetAsync(
              groupName,
              ipName
            );

            //Console.WriteLine("Creating the network interface...");
            return await networkManagementClient.NetworkInterfaces.CreateOrUpdateAsync(
              groupName,
              nicName,
              new NetworkInterface
              {
                  Location = location,
                  IpConfigurations = new List<NetworkInterfaceIPConfiguration>
                    {
             new NetworkInterfaceIPConfiguration
               {
                 Name = nicName,
                 PublicIPAddress = publicIP,
                 Subnet = subnet
               }
                    }
              }
            );
        }

        public static async Task<AvailabilitySet> CreateAvailabilitySetAsync(
          TokenCredentials credential,
          string groupName,
          string subscriptionId,
          string location,
          string avsetName)
        {
            var computeManagementClient = new ComputeManagementClient(credential)
            { SubscriptionId = subscriptionId };

            //Console.WriteLine("Creating the availability set...");
            return await computeManagementClient.AvailabilitySets.CreateOrUpdateAsync(
              groupName,
              avsetName,
              new AvailabilitySet()
              { Location = location }
            );
        }
        public static async Task<VirtualMachine> CreateVirtualMachineAsync(
          TokenCredentials credential,
          string groupName,
          string subscriptionId,
          string location,
          string nicName,
          string avsetName,
          string storageName,
          string adminName,
          string adminPassword,
          string vmName)
        {
            var networkManagementClient = new NetworkManagementClient(credential)
            { SubscriptionId = subscriptionId };
            var computeManagementClient = new ComputeManagementClient(credential)
            { SubscriptionId = subscriptionId };
            var nic = await networkManagementClient.NetworkInterfaces.GetAsync(
              groupName,
              nicName);
            var avSet = await computeManagementClient.AvailabilitySets.GetAsync(
              groupName,
              avsetName);

            //Console.WriteLine("Creating the virtual machine...");
            return await computeManagementClient.VirtualMachines.CreateOrUpdateAsync(
              groupName,
              vmName,

              new VirtualMachine
              {
                  Location = location,
                  AvailabilitySet = new Microsoft.Azure.Management.Compute.Models.SubResource
                  {
                      Id = avSet.Id
                  },
                  HardwareProfile = new HardwareProfile
                  {
                      VmSize = "Standard_A0"
                  },
                  OsProfile = new OSProfile
                  {
                      AdminUsername = adminName,
                      AdminPassword = adminPassword,
                      ComputerName = vmName,
                      WindowsConfiguration = new WindowsConfiguration
                      {
                          ProvisionVMAgent = true
                      }
                  },
                  NetworkProfile = new NetworkProfile
                  {
                      NetworkInterfaces = new List<NetworkInterfaceReference>
                        {
                 new NetworkInterfaceReference { Id = nic.Id }
                        }
                  },
                  StorageProfile = new StorageProfile
                  {
                      ImageReference = new ImageReference
                      {
                          Publisher = "MicrosoftWindowsServer",
                          Offer = "WindowsServer",
                          Sku = "2012-R2-Datacenter",
                          Version = "latest"
                      },
                      OsDisk = new OSDisk
                      {
                          Name = "mytestod1",
                          CreateOption = DiskCreateOptionTypes.FromImage,
                          Vhd = new VirtualHardDisk
                          {
                              Uri = "http://" + storageName + ".blob.core.windows.net/vhds/mytestod1.vhd"
                          }
                      }
                  }
              }
            );
        }




    }
}