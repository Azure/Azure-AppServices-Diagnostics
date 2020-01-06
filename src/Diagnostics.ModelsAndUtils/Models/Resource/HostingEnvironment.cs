using System;
using System.Collections.Generic;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.ScriptUtilities;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class HostingEnvironment : HostingEnvironmentFilter, IResource
    {
        /// <summary>
        /// Name of the Hosting Environment
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Subscription Id(Guid)
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Resource Group Name
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Resource URI
        /// </summary>
        public string ResourceUri
        {
            get
            {
                return UriUtilities.BuildAzureResourceUri(SubscriptionId, ResourceGroup, Name, Provider, ResourceTypeName);
            }
        }

        /// <summary>
        /// Internal Name (For Example:- waws-prod-....)
        /// </summary>
        public string InternalName { get; set; }

        /// <summary>
        /// If the hosting environment is an ASE then this would be the ASE name
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Service Address
        /// </summary>
        public string ServiceAddress { get; set; }

        public int State { get; set; }

        /// <summary>
        /// DNS Suffix
        /// </summary>
        public string DnsSuffix { get; set; }

        /// <summary>
        /// Time since the environment is unhealthy.
        /// </summary>
        public DateTime? UnhealthySince { get; set; }

        /// <summary>
        /// Time indicating when the environment was suspended.
        /// </summary>
        public DateTime? SuspendedOn { get; set; }

        /// <summary>
        /// Boolean representing if the Environment is healthy or not.
        /// </summary>
        public bool IsUnhealthy
        {
            get
            {
                return UnhealthySince != null;
            }
        }

        /// <summary>
        /// Location
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Arm Resource Provider
        /// </summary>
        public string Provider
        {
            get
            {
                return "Microsoft.Web";
            }
        }

        /// <summary>
        /// Name of Resource Type as defined by ARM resource id. Examples: 'sites', 'hostingEnvironments'
        /// </summary>
        public string ResourceTypeName
        {
            get
            {
                return "hostingEnvironments";
            }
        }

        /// <summary>
        /// Subscription Location Placement id
        /// </summary>
        public string SubscriptionLocationPlacementId
        {
            get; set;
        }

        public HostingEnvironment(string subscriptionId, string resourceGroup, string name, string subLocationPlacementId = null)
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroup = resourceGroup;
            this.FriendlyName = name;
            SubscriptionLocationPlacementId = subLocationPlacementId;
        }

        public bool IsApplicable(IResourceFilter filter)
        {
            if (filter is HostingEnvironmentFilter envFilter)
            {
                return ((envFilter.PlatformType & this.PlatformType) > 0) &&
                    ((envFilter.HostingEnvironmentType & this.HostingEnvironmentType) > 0);
            }

            return false;
        }
    }
}
