using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Diagnostics.DataProviders.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.DataProviders
{
    [DataSourceConfiguration("Kusto")]
    public class KustoDataProviderConfiguration : DataProviderConfigurationBase, IDataProviderConfiguration
    {
        /// <summary>
        /// Client Id
        /// </summary>
        [ConfigurationName("ClientId")]
        [Required]
        public string ClientId { get; set; }

        /// <summary>
        /// Subject name of the certificate that will be sent to AAD for token acquisition.
        /// </summary>
        [ConfigurationName("TokenRequestorCertSubjectName")]
        [Required]
        public string TokenRequestorCertSubjectName { get; set; }

        /// <summary>
        /// DB Name
        /// </summary>
        [ConfigurationName("DBName")]
        [Required]
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
        /// DB Name
        /// </summary>
        [ConfigurationName("KustoClusterFailoverGroupings")]
        public string KustoClusterFailoverGroupings { get; set; }

        /// <summary>
        /// DB Name mappings like clusterName|aggClusterName, where clusterName should be the values in KustoClusterNameGroupings
        /// </summary>
        [ConfigurationName("KustoAggClusterNameGroupMappings")]
        public string KustoAggClusterNameGroupMappings { get; set; }

        /// <summary>
        /// DB Name mappings like WawsPrimaryClusterName:DiagLeaderClusterName, where WawsPrimaryClusterName should be one of wawswus,wawseus,wawscus,wawsweu,wawsneu,wawseas
        /// </summary>
        [ConfigurationName("KustoWawsPrimaryToDiagLeaderMappings")]
        public string KustoWawsPrimaryToDiagLeaderMappings { get; set; }

        /// <summary>
        /// Tenant to authenticate with
        /// </summary>
        [ConfigurationName("AADAuthority")]
        [Required]
        public string AADAuthority { get; set; }

        /// <summary>
        /// Resource to issue token
        /// </summary>
        [ConfigurationName("AADKustoResource")]
        [Required]
        public string AADKustoResource { get; set; }

        /// <summary>
        /// Number of consecutive failures before failing over to the fail over cluster.
        /// </summary>
        [ConfigurationName("HeartBeatConsecutiveFailureLimit")]
        public int HeartBeatConsecutiveFailureLimit { get; set; }

        /// <summary>
        /// Number of consecutive successes before returning to the primary cluster.
        /// </summary>
        [ConfigurationName("HeartBeatConsecutiveSuccessLimit")]
        public int HeartBeatConsecutiveSuccessLimit { get; set; }

        /// <summary>
        /// Query to run against each cluster to check health
        /// </summary>
        [ConfigurationName("HeartBeatQuery")]
        public string HeartBeatQuery { get; set; }

        /// <summary>
        /// Timeout of the query
        /// </summary>
        [ConfigurationName("HeartBeatTimeOutInSeconds")]
        public int HeartBeatTimeOutInSeconds { get; set; }

        /// <summary>
        /// Delay between each heart beat
        /// </summary>
        [ConfigurationName("HeartBeatDelayInSeconds")]
        public int HeartBeatDelayInSeconds { get; set; }

        /// <summary>
        /// Region Specific Cluster Names.
        /// </summary>
        public ConcurrentDictionary<string, string> RegionSpecificClusterNameCollection { get; set; }

        /// <summary>
        /// Failover Cluster Names.
        /// </summary>
        public ConcurrentDictionary<string, string> FailoverClusterNameCollection { get; set; }

        /// <summary>
        /// Kusto map.
        /// </summary>
        public IKustoMap KustoMap { get; set; }

        [ConfigurationName("UseKustoMapForPublic")]
        [Required]
        public bool UseKustoMapForPublic { get; set; }

        /// <summary>
        /// Flag to control heart beat query.
        /// </summary>
        [ConfigurationName("EnableHeartBeatQuery")]
        public bool EnableHeartBeatQuery { get; set; }

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
                var m = Regex.Match(AADKustoResource, @"https://(?<cluster>\w+).");
                if (m.Success)
                {
                    return AADKustoResource.Replace(m.Groups["cluster"].Value, "{cluster}");
                }
                else
                {
                    throw new ArgumentException(nameof(AADKustoResource) + " not correctly formatted.");
                }
            }
        }

        /// <summary>
        /// Max Retry Count
        /// </summary>
        [ConfigurationName("Retry:MaxRetryCount")]
        public int MaxRetryCount { get; set; }

        /// <summary>
        /// Delay in Seconds between two retries
        /// </summary>
        [ConfigurationName("Retry:RetryDelayInSeconds")]
        public int RetryDelayInSeconds { get; set; }

        /// <summary>
        /// Flag indicating whether to use Backup cluster (if available) for last retry
        /// </summary>
        [ConfigurationName("Retry:UseBackupClusterForLastAttempt")]
        public bool UseBackupClusterForLastRetryAttempt { get; set; }

        /// <summary>
        /// Maximum Response time for failed requests to qualify for Retry
        /// </summary>
        [ConfigurationName("Retry:MaxFailureResponseTimeInSecondsForRetry")]
        public double MaxFailureResponseTimeInSecondsForRetry { get; set; }

        /// <summary>
        /// List of | separated Exceptions to retry for.
        /// </summary>
        [ConfigurationName("Retry:ExceptionsToRetryFor")]
        public string ExceptionsToRetryFor { get; set; }

        /// <summary>
        /// List of , separated sourceCluster|testCluster1:testCluster2:... and requests will be intercepted from sourceCluster to all the following testClusters.
        /// Will fallback to sourceCluster on any exception
        /// e.g. "wawswusfollower|testwuscluster1:testwuscluster2,wawseusfollower|testeuscluster1"
        /// </summary>
        [ConfigurationName("QueryShadowingClusterMapping")]
        public string QueryShadowingClusterMappingString { get; set; }

        /// <summary>
        /// cluster mappings for data hole fall back
        /// </summary>
        [ConfigurationName("DataHoleFallbackClusterMappings")]
        public string DataHoleFallbackClusterMappingsString { get; set; }

        /// <summary>
        /// Time ranges of data holes, e.g. "2022-03-04 01:00|2022-03-09 01:00", separated by comma
        /// </summary>
        [ConfigurationName("DataHoleTimeRanges")]
        public string DataHoleTimeRangesString { get; set; }

        public ConcurrentDictionary<string, string[]> QueryShadowingClusterMapping;

        public ConcurrentDictionary<string, string> HiPerfAggClusterMapping;

        public ConcurrentDictionary<string, string> WawsPrimaryToDiagLeaderClusterMapping;

        public ConcurrentDictionary<string, string> DataHoleFallbackClusterMappings;

        public ConcurrentBag<(DateTime st, DateTime et)> DataHoleTimeRanges;


        public List<ITuple> OverridableExceptionsToRetryAgainstLeaderCluster { get; set; }

        public IConfiguration config { private get; set; }
        

        public override void PostInitialize()
        {
            RegionSpecificClusterNameCollection = new ConcurrentDictionary<string, string>();
            FailoverClusterNameCollection = new ConcurrentDictionary<string, string>();
            OverridableExceptionsToRetryAgainstLeaderCluster = new List<ITuple>();

            if (string.IsNullOrWhiteSpace(KustoRegionGroupings) && string.IsNullOrWhiteSpace(KustoClusterNameGroupings))
            {
                return;
            }

            var separator = new char[] { ',' };
            var regionGroupingParts = KustoRegionGroupings.Split(separator);
            var clusterNameGroupingParts = KustoClusterNameGroupings.Split(separator);
            var clusterFailoverGroupingParts = string.IsNullOrWhiteSpace(KustoClusterFailoverGroupings) ? new string[0] : KustoClusterFailoverGroupings.Split(separator);

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

                if (iterator < clusterFailoverGroupingParts.Length && !String.IsNullOrWhiteSpace(clusterFailoverGroupingParts[iterator]))
                {
                    FailoverClusterNameCollection.TryAdd(clusterNameGroupingParts[iterator], clusterFailoverGroupingParts[iterator]);
                }
            }

            if (config != null)
            {
                string ExceptionString;
                double MaxFailureResponseTimeInSeconds;

                foreach (var overridableException in config.GetSection("Kusto").GetSection("Retry").GetSection("OverridableExceptionsToRetryAgainstLeaderCluster").GetChildren().ToList())
                {
                    ExceptionString = overridableException.GetSection("ExceptionString").Value;
                    if (double.TryParse(overridableException.GetSection("MaxFailureResponseTimeInSeconds").Value, out MaxFailureResponseTimeInSeconds) 
                        && !string.IsNullOrWhiteSpace(ExceptionString))
                    {
                        OverridableExceptionsToRetryAgainstLeaderCluster.Add((ExceptionString, MaxFailureResponseTimeInSeconds));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(QueryShadowingClusterMappingString))
            {
                try
                {
                    QueryShadowingClusterMapping = new ConcurrentDictionary<string, string[]>(
                           QueryShadowingClusterMappingString
                               .Split(',')
                               .Select(e =>
                               {
                                   var splitted = e.Split('|');
                                   return new KeyValuePair<string, string[]>(splitted[0], splitted[1].Split(':'));
                               }));
                }
                catch (Exception)
                {
                    // swallow the exception
                }
            }

            if (!string.IsNullOrWhiteSpace(KustoAggClusterNameGroupMappings))
            {
                try
                {
                    HiPerfAggClusterMapping = new ConcurrentDictionary<string, string>(GetClusterMappingFromString(KustoAggClusterNameGroupMappings));
                }
                catch (Exception)
                {
                    // swallow the exception
                }
            }

            if (!string.IsNullOrWhiteSpace(KustoWawsPrimaryToDiagLeaderMappings))
            {
                try 
                {
                    WawsPrimaryToDiagLeaderClusterMapping =
                        new ConcurrentDictionary<string, string>(GetClusterMappingFromString(KustoWawsPrimaryToDiagLeaderMappings));
                }
                catch (Exception)
                {
                    // swallow the exception
                }
            }

            if (!string.IsNullOrWhiteSpace(DataHoleFallbackClusterMappingsString))
            {
                try
                {
                    DataHoleFallbackClusterMappings = new ConcurrentDictionary<string, string>(GetClusterMappingFromString(DataHoleFallbackClusterMappingsString));
                }
                catch (Exception)
                {
                    // swallow the exception
                }
            }

            if (!string.IsNullOrWhiteSpace(DataHoleTimeRangesString))
            {
                try
                {
                    DataHoleTimeRanges = new ConcurrentBag<(DateTime st, DateTime et)>(
                        DataHoleTimeRangesString
                            .Split(',')
                            .Select(s => 
                            {
                                var splitted = s.Split('|');
                                DateTime st, et;
                                if (splitted.Length != 2 || !DateTime.TryParse(splitted[0], out st) || !DateTime.TryParse(splitted[1], out et))
                                {
                                    throw new Exception("Malformed time range string pair");
                                }
                                return (st, et);
                            }));
                }
                catch (Exception)
                {
                    // swallow the exception
                }
            }
        }

        private IEnumerable<KeyValuePair<string, string>> GetClusterMappingFromString(string s)
        {
            return s.Split(',')
                    .Select(e =>
                    {
                        var splitted = e.Split('|');
                        return new KeyValuePair<string, string>(splitted[0], splitted[1]);
                    });
        }
    }
}
