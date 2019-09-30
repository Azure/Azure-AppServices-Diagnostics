using System;
using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class NetworkResourceProviderRouteTable
    {
        public string ProvisioningState { get; set; }
        public bool DisableBgpRoutePropagation { get; set; }
        public IEnumerable<NetworkResourceProviderRoute> Routes { get; set; }
        public IEnumerable<NetworkResourceProviderSubnet> Subnets { get; set; }
    }

    public class NetworkResourceProviderRoute
    {
        public string ProvisioningState { get; set; }
        public string NextHopIpAddress { get; set; }
        public string AddressPrefix { get; set; }
        public string NextHopType { get; set; }
    }

    public class NetworkAddressSpace
    {
        public IEnumerable<string> AddressPrefixes { get; set; }
    }

    public class NetworkResourceProviderRouteTableRef
    {
        public string Id { get; set; }
    }

    public class NetworkResourceProviderVirtualNetwork
    {
        public string ProvisioningState { get; set; }
        public string ResourceGuid { get; set; }
        public NetworkAddressSpace AddressSpace { get; set; }
        public IEnumerable<NetworkResourceProviderSubnet> Subnets { get; set; }
    }

    public class NetworkResourceProviderNsgRule : IComparable<NetworkResourceProviderNsgRule>
    {
        public string Protocol { get; set; }
        public string SourcePortRange { get; set; }
        public string DestinationPortRange { get; set; }
        public string SourceAddressPrefix { get; set; }
        public string DestinationAddressPrefix { get; set; }
        public string Access { get; set; }
        public int Priority { get; set; }
        public string Direction { get; set; }

        public int CompareTo(NetworkResourceProviderNsgRule other)
        {
            if (other == null)
            {
                return 1;
            }

            return Priority.CompareTo(other.Priority);
        }
    }

    public class NetworkResourceProviderNsg
    {
        public string ResourceGuid { get; set; }
        public IEnumerable<NetworkResourceProviderNsgRule> SecurityRules { get; set; }
        public IEnumerable<NetworkResourceProviderNsgRule> DefaultSecurityRules { get; set; }
    }

    public class NetworkResourceProviderSubnetNsgRef
    {
        public string Id { get; set; }
    }

    public class NetworkResourceProviderIpConfigurationRef
    {
        public string Id { get; set; }
    }

    public class NetworkResourceProviderServiceEndpointRef
    {
        public string ProvisioningState { get; set; }
        public string Service { get; set; }
        public string[] Locations { get; set; }
    }

    public class NetworkResourceProviderSubnetDelegation
    {
        public string ProvisioningState { get; set; }
        public string ServiceName { get; set; }
        public IEnumerable<string> Actions { get; set; }
    }

    public class NetworkResourceProviderSubnet
    {
        public string ProvisioningState { get; set; }
        public string AddressPrefix { get; set; }

        public NetworkResourceProviderSubnetNsgRef NetworkSecurityGroup { get; set; }
        public NetworkResourceProviderIpConfigurationRef[] IpConfigurations { get; set; }
        public NetworkResourceProviderServiceEndpointRef[] ServiceEndpoints { get; set; }
        public NetworkResourceProviderRouteTableRef RouteTable { get; set; }
        public IEnumerable<NetworkResourceProviderSubnetDelegation> Delegations { get; set; }
    }

    public class VnetConfiguration
    {
        /// <summary>
        /// The name of the test that failed.
        /// </summary>
        public NetworkResourceProviderVirtualNetwork Vnet { get; set; }

        /// <summary>
        /// The name of the test that failed.
        /// </summary>
        public NetworkResourceProviderSubnet Subnet { get; set; }

        /// <summary>
        /// The name of the test that failed.
        /// </summary>
        public NetworkResourceProviderNsg Nsg { get; set; }
    }
}
