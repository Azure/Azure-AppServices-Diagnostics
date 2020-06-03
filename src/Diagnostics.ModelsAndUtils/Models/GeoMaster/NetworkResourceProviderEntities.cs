using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class NetworkResourceProviderVirtualNetwork
    {
        public string ProvisioningState { get; set; }
        public string ResourceGuid { get; set; }
        public NetworkAddressSpace AddressSpace { get; set; }
        public ResponseMessageEnvelope<NetworkResourceProviderSubnet>[] Subnets { get; set; }
        public ResponseMessageEnvelope<NetworkResourceProviderPeering>[] VirtualNetworkPeerings { get; set; }
    }

    public class NetworkAddressSpace
    {
        public string[] AddressPrefixes { get; set; }
    }

    public class NetworkResourceProviderSubnet
    {
        public string ProvisioningState { get; set; }
        public string AddressPrefix { get; set; }

        public NetworkResourceProviderId NetworkSecurityGroup { get; set; }
        public NetworkResourceProviderId RouteTable { get; set; }
        public NetworkResourceProviderServiceEndpointRef[] ServiceEndpoints { get; set; }
        public NetworkResourceProviderId[] IpConfigurations { get; set; }
        public ResponseMessageEnvelope<NetworkResourceProviderSubnetDelegation>[] Delegations { get; set; }
        public ResponseMessageEnvelope<NetworkResourceProviderResourceNavigationLink>[] ResourceNavigationLinks { get; set; }
    }

    public class NetworkResourceProviderPeering
    {
        public string ProvisioningState { get; set; }
        public string PeeringState { get; set; }
        public NetworkResourceProviderId RemoteVirtualNetwork { get; set; }
        public NetworkAddressSpace RemoteAddressSpace { get; set; }
        public bool AllowVirtualNetworkAccess { get; set; }
        public bool AllowForwardedTraffic { get; set; }
        public bool AllowGatewayTransit { get; set; }
        public bool UseRemoteGateways { get; set; }
        public bool DoNotVerifyRemoteGateways { get; set; }
    }

    public class NetworkResourceProviderId
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
        public List<string> Actions { get; set; }
    }

    public class NetworkResourceProviderResourceNavigationLink
    {
        public string ProvisioningState { get; set; }
        public string LinkedResourceType { get; set; }
        public string Link { get; set; }
    }

    public class NetworkResourceProviderRouteTable
    {
        public string ProvisioningState { get; set; }
        public bool DisableBgpRoutePropagation { get; set; }
        public ResponseMessageEnvelope<NetworkResourceProviderRoute>[] Routes { get; set; }
        public NetworkResourceProviderId[] Subnets { get; set; }
    }

    public class NetworkResourceProviderRoute
    {
        public string ProvisioningState { get; set; }
        public string NextHopIpAddress { get; set; }
        public string AddressPrefix { get; set; }
        public string NextHopType { get; set; }
        public string HasBgpOverride { get; set; }
    }

    public class NetworkResourceProviderNsg
    {
        public string ResourceGuid { get; set; }
        public string ProvisioningState { get; set; }

        public ResponseMessageEnvelope<NetworkResourceProviderNsgRule>[] SecurityRules { get; set; }
        public ResponseMessageEnvelope<NetworkResourceProviderNsgRule>[] DefaultSecurityRules { get; set; }
        public NetworkResourceProviderId[] Subnets { get; set; }
    }

    public class NetworkResourceProviderNsgRule : IComparable<NetworkResourceProviderNsgRule>
    {
        public string ProvisioningState { get; set; }
        public string Protocol { get; set; }
        public string SourcePortRange { get; set; }
        public string DestinationPortRange { get; set; }
        public string SourceAddressPrefix { get; set; }
        public string DestinationAddressPrefix { get; set; }
        public NetworkResourceProviderId[] SourceApplicationSecurityGroups { get; set; }
        public NetworkResourceProviderId[] DestinationApplicationSecurityGroups { get; set; }
        public string[] SourcePortRanges { get; set; }
        public string[] DestinationPortRanges { get; set; }
        public string[] SourceAddressPrefixes { get; set; }
        public string[] DestinationAddressPrefixes { get; set; }
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

    public class EffectiveRoute
    {
        public string Name { get; set; }

        public RouteSource Source { get; set; }

        public RouteStatus Status { get; set; }

        public string[] AddressPrefixes { get; set; }

        public EffectiveNextHop EffectiveNextHop { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RouteSource
    {
        User,
        VPNGateway,
        Default
    };

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RouteStatus
    {
        Active, Invalid
    };

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RouteType
    {
        VPNGateway,
        VNETLocal,
        Internet,
        VirtualAppliance,
        Null
    };

    public class EffectiveNextHop
    {
        public RouteType Type { get; set; }

        public string[] IpAddresses { get; set; }
    }
    

    public class EffectiveRouteTable
    {
        public EffectiveRoute[] EffectiveRoutes { get; set; }
    }

    /// <summary>
    /// Message envelope that contains the common Azure resource manager properties and the resource provider specific content.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResponseMessageEnvelope<T>
    {
        /// <summary>
        /// Resource Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of resource e.g "Microsoft.Web/sites".
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Kind of app e.g web app, api app, mobile app.
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Geographical region resource belongs to e.g. SouthCentralUS, SouthEastAsia.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Tags associated with resource.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Resource specific properties.
        /// </summary>
        public T Properties { get; set; }
    }

    public class VnetConfiguration
    {
        /// <summary>
        /// The virtual network
        /// </summary>
        public ResponseMessageEnvelope<NetworkResourceProviderVirtualNetwork> Vnet { get; set; }

        /// <summary>
        /// The subnet within the virtual network
        /// </summary>
        public ResponseMessageEnvelope<NetworkResourceProviderSubnet> Subnet { get; set; }

        /// <summary>
        /// The Network Security Group associated with the subnet
        /// </summary>
        public ResponseMessageEnvelope<NetworkResourceProviderNsg> Nsg { get; set; }

        /// <summary>
        /// The Route Table associated with the subnet
        /// </summary>
        public ResponseMessageEnvelope<NetworkResourceProviderRouteTable> RouteTable { get; set; }

        /// <summary>
        /// The effective routes that are applied to the subnet's App Service Environment, if one exists
        /// </summary>
        public EffectiveRouteTable EffectiveRoutes { get; set; }
    }
}
