﻿using System;
using System.Collections.Concurrent;

namespace Diagnostics.DataProviders
{
    [DataSourceConfiguration("Kusto")]
    public class KustoDataProviderConfiguration : IDataProviderConfiguration
    {
        /// <summary>
        /// Client Id
        /// </summary>
        [ConfigurationName("ClientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// App Key
        /// </summary>
        [ConfigurationName("AppKey")]
        public string AppKey { get; set; }

        /// <summary>
        /// DB Name
        /// </summary>
        [ConfigurationName("DBName")]
        public string DBName { get; set; }

        /// <summary>
        /// DB Name
        /// </summary>
        [ConfigurationName("KustoRegionGroupings")]
        public string KustoRegionGroupings { get; set; }

        /// <summary>
        /// DB Name
        /// </summary>
        [ConfigurationName("KustoClusterNameGroupings")]
        public string KustoClusterNameGroupings { get; set; }

        /// <summary>
        /// Tenant to authenticate with
        /// </summary>
        [ConfigurationName("AADAuthority")]
        public string AADAuthority { get; set; }

        /// <summary>
        /// Resource to issue token
        /// </summary>
        [ConfigurationName("AADKustoResource")]
        public string AADKustoResource { get; set; }

        /// <summary>
        /// Region Specific Cluster Names.
        /// </summary>
        public ConcurrentDictionary<string, string> RegionSpecificClusterNameCollection { get; set; }

        public string CloudDomain
        {
            get
            {
                if (AADKustoResource.Contains("windows.net"))
                {
                    return DataProviderConstants.AzureCloud;
                }
                else if (AADKustoResource.Contains("chinacloudapi.cn"))
                {
                    return DataProviderConstants.AzureChinaCloud;
                }
                else
                {
                    return DataProviderConstants.AzureUSGovernment;
                }
            }
        }

        public string KustoApiEndpoint
        {
            get
            {
                var uri = new Uri(AADKustoResource);
                var host = uri.Host;
                return uri.OriginalString.Replace(host, "{cluster}");
            }
        }

        public KustoDataProviderConfiguration()
        {

        }

        public void PostInitialize()
        {
            RegionSpecificClusterNameCollection = new ConcurrentDictionary<string, string>();

            if (string.IsNullOrWhiteSpace(KustoRegionGroupings) && string.IsNullOrWhiteSpace(KustoClusterNameGroupings))
            {
                return;
            }

            var separator = new char[] { ',' };
            var regionGroupingParts = KustoRegionGroupings.Split(separator);
            var clusterNameGroupingParts = KustoClusterNameGroupings.Split(separator);

            if (regionGroupingParts.Length != clusterNameGroupingParts.Length)
            {
                // TODO: Log
                return;
            }

            for (int iterator = 0; iterator < regionGroupingParts.Length; iterator++)
            {
                var regionParts = regionGroupingParts[iterator].Split(new char[] { ':' });

                foreach (var region in regionParts)
                {
                    if (!String.IsNullOrWhiteSpace(region))
                    {
                        RegionSpecificClusterNameCollection.TryAdd(region.ToLower(), clusterNameGroupingParts[iterator]);
                    }
                }
            }
        }

    }
}
