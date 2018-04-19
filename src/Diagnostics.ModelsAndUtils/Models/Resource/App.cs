using Diagnostics.ModelsAndUtils.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Resource representing an app service
    /// </summary>
    public class App : AppFilter, IResource
    {
        /// <summary>
        /// Name of the App (For example :- foobar, foobar(staging) ...)
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

        public App(string subscriptionId, string resourceGroup, string appName) : base()
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroup = resourceGroup;
            this.Name = appName;
        }

        // <summary>
        /// Determines whether the app resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">App Resource Filter</param>
        /// <returns>True, if app resource passes the filter. False otherwise</returns>
        public bool IsApplicable(IResourceFilter filter)
        {
            if(filter is AppFilter appFilter)
            {
                return ((appFilter.AppType & this.AppType) > 0) &&
                    ((appFilter.PlatformType & this.PlatformType) > 0) &&
                    ((appFilter.StackType & this.StackType) > 0) &&
                    ((appFilter.StampType & this.StampType) > 0);
            }

            return false;
        }
    }
}
