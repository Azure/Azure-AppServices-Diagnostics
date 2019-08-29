using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Diagnostics.Logger;
using Microsoft.AspNetCore.Http.Features;
using System.Data;

namespace Diagnostics.DataProviders
{
    public interface IKustoHeartBeatService : IDisposable
    {
        Task<string> GetClusterNameFromStamp(string stampName);
    }

    public class KustoHeartBeatService : IKustoHeartBeatService
    {
        private KustoDataProviderConfiguration _configuration;
        private Dictionary<string, KustoHeartBeat> _heartbeats;
        private KustoDataProvider _kustoDataProvider;
        private CancellationTokenSource _cancellationToken;

        private void Initialize()
        {
            _heartbeats = new Dictionary<string, KustoHeartBeat>();
            _kustoDataProvider = new KustoDataProvider(new OperationDataCache(), _configuration, Guid.NewGuid().ToString(), this);
            _cancellationToken = new CancellationTokenSource();


            foreach (var primaryCluster in _configuration.RegionSpecificClusterNameCollection.Values)
            {
                string failoverCluster = null;
                if  (_configuration.FailoverClusterNameCollection.ContainsKey(primaryCluster))
                {
                    failoverCluster = _configuration.FailoverClusterNameCollection[primaryCluster];
                }
                _heartbeats[primaryCluster] = new KustoHeartBeat(primaryCluster, failoverCluster, _kustoDataProvider, _configuration);
                // Start threads for each heartbeat on the thread pool
                Task.Run(() => _heartbeats[primaryCluster].RunHeartBeatTask(_cancellationToken.Token));
            }
        }


        public void Dispose()
        {
            _cancellationToken.Cancel();
        }

        public KustoHeartBeatService(KustoDataProviderConfiguration configuration)
        {
            _configuration = configuration;
            Initialize();
        }

        public async Task<string> GetClusterNameFromStamp(string stampName)
        {
            string kustoClusterName = null;
            string appserviceRegion = ParseRegionFromStamp(stampName);

            if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue(appserviceRegion.ToLower(), out kustoClusterName))
            {
                if(_configuration.CloudDomain == DataProviderConstants.AzureCloud)
                {
                    try
                    {
                         DataTable regionMappings = await _kustoDataProvider.ExecuteClusterQuery($"cluster('wawseusfollower').database('wawsprod').WawsAn_regionsincluster | where pdate >= ago(5d) | summarize by Region, ClusterName", "RegionMappingInit");
                        if (regionMappings.Rows.Count > 0)
                        {
                            foreach (DataRow dr in regionMappings.Rows)
                            {
                                _configuration.RegionSpecificClusterNameCollection.TryAdd(((string)dr["Region"]).ToLower(), $"{((string)dr["ClusterName"])}follower");
                                _configuration.FailoverClusterNameCollection.TryAdd(((string)dr["Region"]).ToLower(), (string)dr["ClusterName"]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Swallow any Kusto related exception, then check if a mapping exists for *. Throw an exception is not.
                    }
                    finally
                    {
                        if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue(appserviceRegion.ToLower(), out kustoClusterName) && !_configuration.RegionSpecificClusterNameCollection.TryGetValue("*", out kustoClusterName))
                        {
                            throw new KeyNotFoundException(String.Format("Kusto Cluster Name not found for Region : {0}", appserviceRegion.ToLower()));
                        }
                    }
                }                 
            }

            if (!string.IsNullOrWhiteSpace(_heartbeats[kustoClusterName].FailoverCluster))
            {
                if (!_heartbeats[kustoClusterName].UsePrimaryCluster)
                {
                    kustoClusterName = _heartbeats[kustoClusterName].FailoverCluster;
                }
            }

            return kustoClusterName;
        }

        private static string ParseRegionFromStamp(string stampName)
        {
            if (string.IsNullOrWhiteSpace(stampName))
            {
                throw new ArgumentNullException(nameof(stampName));
            }

            var stampParts = stampName.Split(new char[] { '-' });
            if (stampParts.Any() && stampParts.Length >= 3)
            {
                return stampParts[2];
            }

            //return * for private stamps if no prod stamps are found
            return "*";
        }
    }

    public class KustoHeartBeat
    {
        public string PrimaryCluster { get; }
        public string FailoverCluster { get; }
        public bool UsePrimaryCluster { get; private set; }
        private int _ConsecutiveFailureCount = 0;
        private int _ConsecutiveSuccessCount = 0;
        private KustoDataProvider _kustoDataProvider;
        private KustoDataProviderConfiguration _configuration;

        public KustoHeartBeat(string primaryCluster, string failoverCluster, KustoDataProvider kustoDataProvider, KustoDataProviderConfiguration configuration)
        {
            PrimaryCluster = primaryCluster;
            FailoverCluster = failoverCluster;
            UsePrimaryCluster = true;
            _kustoDataProvider = kustoDataProvider;
            _configuration = configuration;
        }

        private async Task RunHeartBeatPrimary(string activityId)
        {
            bool primaryHeartBeatSuccess = false;
            Exception exceptionForLog = null;
            try
            {
                var primaryHeartBeat = await _kustoDataProvider.ExecuteQueryForHeartbeat(_configuration.HeartBeatQuery, PrimaryCluster, _configuration.HeartBeatTimeOutInSeconds, activityId);

                if (primaryHeartBeat.Rows.Count >= 1)
                {
                    primaryHeartBeatSuccess = true;
                }
            }
            catch (Exception ex)
            {
                exceptionForLog = ex;
            }

            if (primaryHeartBeatSuccess)
            {
                _ConsecutiveFailureCount = 0;
                _ConsecutiveSuccessCount++;
            }
            else
            {
                _ConsecutiveFailureCount++;
                _ConsecutiveSuccessCount = 0;
            }

            // if not in failover state
            //  should failover?
            if (UsePrimaryCluster && _ConsecutiveFailureCount >= _configuration.HeartBeatConsecutiveFailureLimit)
            {
                UsePrimaryCluster = false;
            } // else should stop failover
            else if (!UsePrimaryCluster && _ConsecutiveSuccessCount >= _configuration.HeartBeatConsecutiveSuccessLimit)
            {
                UsePrimaryCluster = true;
            }

            LogHeartBeatInformation("Primary", primaryHeartBeatSuccess, PrimaryCluster, activityId, UsePrimaryCluster, exceptionForLog);
        }

        private async Task RunHeartBeatFailover(string activityId)
        {
            bool failoverHeartBeatSuccess = false;
            Exception exceptionForLog = null;
            try
            {
                var failoverHeartBeat = await _kustoDataProvider.ExecuteQueryForHeartbeat(_configuration.HeartBeatQuery, FailoverCluster, _configuration.HeartBeatTimeOutInSeconds, activityId);

                if (failoverHeartBeat.Rows.Count >= 1)
                {
                    failoverHeartBeatSuccess = true;
                }
            }
            catch (Exception ex)
            {
                exceptionForLog = ex;
            }

            LogHeartBeatInformation("Failover", failoverHeartBeatSuccess, FailoverCluster, activityId, UsePrimaryCluster, exceptionForLog);
        }

        public async Task RunHeartBeatTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested) {
                
                string activityId = Guid.NewGuid().ToString();

                if (string.IsNullOrWhiteSpace(FailoverCluster))
                {
                    await RunHeartBeatPrimary(activityId);
                }
                else
                {
                    var primaryTask = RunHeartBeatPrimary(activityId);
                    var failoverTask = RunHeartBeatFailover(activityId);
                    await Task.WhenAll(new Task[] { primaryTask, failoverTask });
                }
                await Task.Delay(TimeSpan.FromSeconds(_configuration.HeartBeatDelayInSeconds), cancellationToken);
            }
        }

        private void LogHeartBeatInformation(string primaryOrFailover, bool clusterSuccess, string cluster, string activityId, bool usingPrimaryCluster, Exception exception)
        {
            var clusterStatus = clusterSuccess ? "Success" : "Failed";

            DiagnosticsETWProvider.Instance.LogKustoHeartbeatInformation(
               activityId,
               $"ClusterType:{primaryOrFailover},ClusterStatus:{clusterStatus},Cluster:{cluster},UsingPrimaryCluster:{usingPrimaryCluster}",
               exception != null ? exception.GetType().ToString() : string.Empty,
               exception != null ? exception.ToString() : string.Empty);
        }
    }
}