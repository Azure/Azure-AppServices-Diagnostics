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

        public IEnumerable<string> Hostnames { get; set; }

        /// <summary>
        /// Primary Stamp where application is deployed.
        /// </summary>
        public HostingEnvironment Stamp { get; set; }

        public App() : base()
        {
        }
    }
}
