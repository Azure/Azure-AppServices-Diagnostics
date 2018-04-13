using Diagnostics.ModelsAndUtils.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

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
        /// Internal Name (For Example:- waws-prod-....)
        /// </summary>
        public string InternalName { get; set; }

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
        /// List of Tenant Ids for this environment.
        /// </summary>
        public IEnumerable<string> TenantIdList;
    }
}
