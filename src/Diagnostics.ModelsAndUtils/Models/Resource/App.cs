using System;
using System.Collections.Generic;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.ScriptUtilities;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Resource representing an app service
    /// </summary>
    public class App : AppFilter, IResource
    {
        /// <summary>
        /// Name of the App
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Slot name
        /// </summary>
        public string Slot { get; private set; }

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
                var resourceName = Name;
                if (!string.IsNullOrWhiteSpace(Slot) && !Slot.Equals("production", StringComparison.OrdinalIgnoreCase) == false)
                {
                    resourceName = $"{resourceName}({Slot})";
                }

                return UriUtilities.BuildAzureResourceUri(SubscriptionId, ResourceGroup, resourceName, Provider, ResourceTypeName);
            }
        }

        /// <summary>
        /// WebSpace name for the app.
        /// </summary>
        public string WebSpace { get; set; }

        /// <summary>
        /// Default Hostname of the app.
        /// </summary>
        public string DefaultHostName { get; set; }

        /// <summary>
        /// Scm Hostname of the app.
        /// </summary>
        public string ScmSiteHostname { get; set; }

        /// <summary>
        /// Hostnames associated with the app.
        /// </summary>
        public IEnumerable<string> Hostnames { get; set; }

        /// <summary>
        /// Primary Stamp where app is deployed.
        /// </summary>
        public HostingEnvironment Stamp { get; set; }

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
                return "sites";
            }
        }

        public string SubscriptionLocationPlacementId
        {
            get; set;
        }

        public App(string subscriptionId, string resourceGroup, string appName, string subLocationPlacementId = null) : base()
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroup = resourceGroup;
            var appNameAndSlot = appName.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            this.Name = appNameAndSlot[0];
            if (appNameAndSlot.Length > 1)
            {
                this.Slot = appNameAndSlot[1].ToLower();
            }
            else
            {
                this.Slot = "production";
            }
        }

        /// <summary>
        /// Determines whether the app resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">App Resource Filter</param>
        /// <returns>True, if app resource passes the filter. False otherwise</returns>
        public bool IsApplicable(IResourceFilter filter)
        {
            if (filter is AppFilter appFilter)
            {
                return ((appFilter.AppType & this.AppType) > 0) &&
                    ((appFilter.PlatformType & this.PlatformType) > 0) &&
                    ((this.StackType == StackType.None) || (appFilter.StackType & this.StackType) > 0) &&
                    ((appFilter.StampType & this.StampType) > 0);
            }

            return false;
        }
    }
}
