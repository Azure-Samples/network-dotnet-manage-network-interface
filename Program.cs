// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;

namespace ManageNetworkInterface
{
    public class Program
    {
        private static ResourceIdentifier? _resourceGroupId = null;

        /**
         * Azure Network sample for managing network interfaces -
         *  - Create a virtual machine with multiple network interfaces
         *  - Configure a network interface
         *  - List network interfaces
         *  - Delete a network interface.
         */
        public static async Task RunSample(ArmClient client)
        {

            string vnetName = Utilities.CreateRandomName("vnet");
            string nicName1 = Utilities.CreateRandomName("nic1-");
            string nicName2 = Utilities.CreateRandomName("nic2-");
            string nicName3 = Utilities.CreateRandomName("nic3-");
            string publicIPAddressLeafDNS1 = Utilities.CreateRandomName("pip1-");
            string publicIPAddressLeafDNS2 = Utilities.CreateRandomName("pip2-");
            string vmName = Utilities.CreateRandomName("vm");

            try
            {
                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                // Create a resource group in the EastUS region
                string rgName = Utilities.CreateRandomName("NetworkSampleRG");
                Utilities.Log($"Creating resource group...");
                ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);

                //============================================================
                // Create a virtual machine with multiple network interfaces

                // Define a virtual network for the VMs in this availability set

                Utilities.Log("Creating a virtual network ...");
                VirtualNetworkData vnetInput = new VirtualNetworkData()
                {
                    Location = resourceGroup.Data.Location,
                    AddressPrefixes = { "172.16.0.0/16" },
                    Subnets =
                    {
                        new SubnetData() { Name = "Front-end", AddressPrefix = "172.16.1.0/24" },
                        new SubnetData() { Name = "Mid-tier", AddressPrefix = "172.16.2.0/24" },
                        new SubnetData() { Name = "Back-end", AddressPrefix = "172.16.3.0/24" },
                    },
                };
                var vnetLro = await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(WaitUntil.Completed, vnetName, vnetInput);
                VirtualNetworkResource vnet = vnetLro.Value;
                Utilities.Log($"Created a virtual network: {vnet.Data.Name}");

                Utilities.Log("Created two public ip...");
                PublicIPAddressResource pip1 = await Utilities.CreatePublicIP(resourceGroup, publicIPAddressLeafDNS1);
                PublicIPAddressResource pip2 = await Utilities.CreatePublicIP(resourceGroup, publicIPAddressLeafDNS2);
                Utilities.Log($"Created public ip: {pip1.Data.Name}");
                Utilities.Log($"Created public ip: {pip2.Data.Name}");

                Utilities.Log("Creating multiple network interfaces...");
                Utilities.Log("Creating network interface 1...");

                var nicInput1 = new NetworkInterfaceData()
                {
                    Location = resourceGroup.Data.Location,
                    EnableIPForwarding = true,
                    IPConfigurations =
                    {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "default-config",
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            Subnet = new SubnetData()
                            {
                                Id = vnet.Data.Subnets.First(item => item.Name == "Front-end").Id
                            },
                            PublicIPAddress = new PublicIPAddressData
                            {
                                Id = pip1.Id,
                            }
                        }
                    }
                };
                var networkInterfaceLro1 = await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(WaitUntil.Completed, nicName1, nicInput1);
                NetworkInterfaceResource nic1 = networkInterfaceLro1.Value;
                Utilities.Log($"Created network interface 1: {nic1.Data.Name}");
                Utilities.Log($"{nic1.Data.Name} associated public ip: {nic1.Data.IPConfigurations.First().PublicIPAddress.Id.Name}");

                Utilities.Log("Creating network interface 2");
                var nicInput2 = new NetworkInterfaceData()
                {
                    Location = resourceGroup.Data.Location,
                    IPConfigurations =
                    {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "default-config",
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            Subnet = new SubnetData()
                            {
                                Id = vnet.Data.Subnets.First(item => item.Name == "Mid-tier").Id
                            }
                        }
                    }
                };
                var networkInterfaceLro2 = await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(WaitUntil.Completed, nicName2, nicInput2);
                NetworkInterfaceResource nic2 = networkInterfaceLro2.Value;
                Utilities.Log($"Created network interface 2: {nic2.Data.Name}");

                Utilities.Log("Creating network interface 3");
                var nicInput3 = new NetworkInterfaceData()
                {
                    Location = resourceGroup.Data.Location,
                    IPConfigurations =
                    {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "default-config",
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            Subnet = new SubnetData()
                            {
                                Id = vnet.Data.Subnets.First(item => item.Name == "Back-end").Id
                            }
                        }
                    }
                };
                var networkInterfaceLro3 = await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(WaitUntil.Completed, nicName3, nicInput3);
                NetworkInterfaceResource nic3 = networkInterfaceLro3.Value;
                Utilities.Log($"Created network interface 3: {nic3.Data.Name}");

                //=============================================================
                // Create a virtual machine with multiple network interfaces

                Utilities.Log("Creating a Windows VM");

                VirtualMachineData vmInput = Utilities.GetDefaultVMInputData(resourceGroup, vmName);
                vmInput.NetworkProfile.NetworkInterfaces.Add(
                    new VirtualMachineNetworkInterfaceReference()
                    {
                        Id = nic1.Id,
                        Primary = true
                    });
                vmInput.NetworkProfile.NetworkInterfaces.Add(
                    new VirtualMachineNetworkInterfaceReference()
                    {
                        Id = nic2.Id,
                        Primary = false
                    });
                vmInput.NetworkProfile.NetworkInterfaces.Add(
                    new VirtualMachineNetworkInterfaceReference()
                    {
                        Id = nic3.Id,
                        Primary = false
                    });
                var vmLro = await resourceGroup.GetVirtualMachines().CreateOrUpdateAsync(WaitUntil.Completed, vmName, vmInput);
                VirtualMachineResource vm = vmLro.Value;
                Utilities.Log("Created VM: " + vm.Data.Name);

                // ===========================================================
                // Configure a network interface
                Utilities.Log("Updating the first network interface");
                var updateNic1Input = nic1.Data;
                updateNic1Input.IPConfigurations.First().PublicIPAddress.Id = pip2.Id;
                networkInterfaceLro1 = await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(WaitUntil.Completed, nicName1, updateNic1Input);
                nic1 = networkInterfaceLro1.Value;
                Utilities.Log($"{nic1.Data.Name} associated public ip: {nic1.Data.IPConfigurations.First().PublicIPAddress.Id.Name}");
                Utilities.Log("Updated the first network interface");

                //============================================================
                // List network interfaces

                Utilities.Log("Walking through network inter4faces in resource group: " + rgName);
                await foreach (var networkInterface in resourceGroup.GetNetworkInterfaces().GetAllAsync())
                {
                    Utilities.Log(networkInterface.Data.Name);
                }

                //============================================================
                // Delete a network interface

                Utilities.Log("Deleting a network interface: " + nic2.Data.Name);
                Utilities.Log("First, deleting the vm");
                await vm.DeleteAsync(WaitUntil.Completed);
                Utilities.Log("Second, deleting the network interface");
                await nic2.DeleteAsync(WaitUntil.Completed);
                Utilities.Log("Deleted network interface");

                Utilities.Log("============================================================");
                Utilities.Log("Remaining network interfaces are ...");
                await foreach (var networkInterface in resourceGroup.GetNetworkInterfaces().GetAllAsync())
                {
                    Utilities.Log(networkInterface.Data.Name);
                }
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group...");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId.Name}");
                    }
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex);
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
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }
    }
}