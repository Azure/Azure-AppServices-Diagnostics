using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Diagnostics.Logger;
using System.Data;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;

namespace Diagnostics.DataProviders
{
    public interface IKustoHeartBeatService : IDisposable
    {
        Task<string> GetClusterNameFromStamp(string stampName);
    }

    public class KustoHeartBeatService : IKustoHeartBeatService
    {
        private readonly KustoDataProviderConfiguration _configuration;
        private readonly ConcurrentDictionary<string, KustoHeartBeat> _heartbeats;
        private readonly KustoDataProvider _kustoDataProvider;
        private readonly CancellationTokenSource _cancellationToken;
        private readonly bool runHeartBeatQuery;

        private void Initialize()
        {
            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage("KustoHeartBeatService.Initialize()");

            foreach (var primaryCluster in _configuration.RegionSpecificClusterNameCollection.Values)
            {
                string failoverCluster = null;
                if  (_configuration.FailoverClusterNameCollection.ContainsKey(primaryCluster))
                {
                    failoverCluster = _configuration.FailoverClusterNameCollection[primaryCluster];
                }
                if (!_heartbeats.ContainsKey(primaryCluster) && _heartbeats.TryAdd(primaryCluster, new KustoHeartBeat(primaryCluster, failoverCluster, _kustoDataProvider, _configuration)) && runHeartBeatQuery)
                {
                    // Start threads for each heartbeat on the thread pool
                    Task.Run(() => _heartbeats[primaryCluster].RunHeartBeatTask(_cancellationToken.Token));
                }
            }
        }

        public void Dispose()
        {
            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage("Disposing KustoHeartBeatService");
            _cancellationToken.Cancel();
        }

        public KustoHeartBeatService(KustoDataProviderConfiguration configuration)
        {
            _configuration = configuration;
            _heartbeats = new ConcurrentDictionary<string, KustoHeartBeat>();
            _kustoDataProvider = new KustoDataProvider(new OperationDataCache(), _configuration, Guid.NewGuid().ToString(), this);
            _cancellationToken = new CancellationTokenSource();

            runHeartBeatQuery = _configuration.EnableHeartBeatQuery;
            Initialize();
        }

        public async Task<string> GetClusterNameFromStamp(string stampName)
        {
            string kustoClusterName = null;
            string appserviceRegion = ParseRegionFromStamp(stampName);

            if (!_configuration.CloudDomain.Equals(DataProviderConstants.AzureCloud, StringComparison.CurrentCultureIgnoreCase) && 
                !_configuration.CloudDomain.Equals(DataProviderConstants.AzureCloudAlternativeName, StringComparison.CurrentCultureIgnoreCase) &&
                stampName.Equals(DataProviderConstants.FakeStampForAnalyticsCluster, StringComparison.CurrentCultureIgnoreCase))
            {
                kustoClusterName = _configuration.RegionSpecificClusterNameCollection.Values.First();
                return kustoClusterName;
            }

            if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue(appserviceRegion.ToLower(), out kustoClusterName))
            {
                if(_configuration.CloudDomain == DataProviderConstants.AzureCloud)
                {
                    DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"{appserviceRegion} wasn't found in region mappings. Populating mappings from kusto.");

                    try
                    {
                         DataTable regionMappings = await _kustoDataProvider.ExecuteClusterQuery($"cluster('wawseusfollower').database('wawsprod').WawsAn_regionsincluster | where pdate >= ago(5d) | summarize by Region, ClusterName = replace('follower', '', ClusterName)", "RegionMappingInit");
                        if (regionMappings.Rows.Count > 0)
                        {
                            foreach (DataRow dr in regionMappings.Rows)
                            {
                                var regionName = ((string)dr["Region"]).ToLower();
                                var clusterName = (string)dr["ClusterName"];
                                _configuration.RegionSpecificClusterNameCollection.TryAdd(regionName, clusterName + "follower");
                                _configuration.FailoverClusterNameCollection.TryAdd(regionName, clusterName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Swallow any Kusto related exception, then check if a mapping exists for *. Throw an exception if not.
                    }
                    finally
                    {
                        if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue(appserviceRegion.ToLower(), out kustoClusterName) && !_configuration.RegionSpecificClusterNameCollection.TryGetValue("*", out kustoClusterName))
                        {
                            throw new KeyNotFoundException(String.Format("Kusto Cluster Name not found for Region : {0}", appserviceRegion.ToLower()));
                        }
                        else
                        {
                            Initialize();
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

        private async Task RunHeartBeatPrimary(string activityId, uint sequenceNumber)
        {
            bool primaryHeartBeatSuccess = false;
            Exception exceptionForLog = null;
            try
            {
                var primaryHeartBeat = await _kustoDataProvider.ExecuteQueryForHeartbeat(_configuration.HeartBeatQuery, PrimaryCluster, _configuration.HeartBeatTimeOutInSeconds, activityId, "PrimaryHealthPing");

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

            /*
             * Logic of failing over to failover cluster:
             *  1. If currently Primary cluster is used
             *  2. If the heartbeat failure count is more than the failure limit
             *  3. When both #1 and #2 are TRUE, run a sample heartbeat query to failover cluster,
             *     and if that succeeds, go ahead with the failover. 
            */
            if (UsePrimaryCluster && _ConsecutiveFailureCount >= _configuration.HeartBeatConsecutiveFailureLimit)
            {
                // Check if heartbeat query succeeds on failover cluster
                bool isFailoverHeartbeatSuccessful = await RunHeartBeatFailover(activityId, sequenceNumber);
                UsePrimaryCluster = !isFailoverHeartbeatSuccessful;
            } 
            // else Stop the failover
            else if (!UsePrimaryCluster && _ConsecutiveSuccessCount >= _configuration.HeartBeatConsecutiveSuccessLimit)
            {
                UsePrimaryCluster = true;
            }

            LogHeartBeatInformation("Primary", primaryHeartBeatSuccess, PrimaryCluster, activityId, sequenceNumber, UsePrimaryCluster, exceptionForLog);
        }

        private async Task<bool> RunHeartBeatFailover(string activityId, uint sequenceNumber)
        {
            bool failoverHeartBeatSuccess = false;
            Exception exceptionForLog = null;
            try
            {
                var failoverHeartBeat = await _kustoDataProvider.ExecuteQueryForHeartbeat(_configuration.HeartBeatQuery, FailoverCluster, _configuration.HeartBeatTimeOutInSeconds, activityId, "FailoverHealthPing");

                if (failoverHeartBeat.Rows.Count >= 1)
                {
                    failoverHeartBeatSuccess = true;
                }
            }
            catch (Exception ex)
            {
                exceptionForLog = ex;
            }

            LogHeartBeatInformation("Failover", failoverHeartBeatSuccess, FailoverCluster, activityId, sequenceNumber, UsePrimaryCluster, exceptionForLog);
            return failoverHeartBeatSuccess;
        }

        public async Task RunHeartBeatTask(CancellationToken cancellationToken)
        {
            string parentActivityId = Guid.NewGuid().ToString();

            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage(
                $"Start HeartBeatTask: primary {PrimaryCluster} failover {FailoverCluster} parentActivityId {parentActivityId} delayInSeconds {_configuration.HeartBeatDelayInSeconds}");

            uint sequenceNumber = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string activityId = Guid.NewGuid().ToString();
                    await RunHeartBeatPrimary(parentActivityId + ":" + activityId, sequenceNumber++);
                    await Task.Delay(TimeSpan.FromSeconds(_configuration.HeartBeatDelayInSeconds), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break; // Graceful exiting
                }
                catch (Exception ex)
                {
                    // This task isn't awaited, hence any unhandled exception won't be caught. So we log it here.
                    DiagnosticsETWProvider.Instance.LogRuntimeHostUnhandledException(
                        parentActivityId, "LogException_RunHeartBeatTask", "", "", "", ex.GetType().ToString(), ex.ToString());
                    break;
                }
            }

            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage(
            $"Exiting HeartBeatTask: primary {PrimaryCluster}, failover {FailoverCluster}, parentActivityId {parentActivityId} delayInSeconds {_configuration.HeartBeatDelayInSeconds}");
        }

        private void LogHeartBeatInformation(string primaryOrFailover, bool clusterSuccess, string cluster, string activityId, uint sequenceNumber, bool usingPrimaryCluster, Exception exception)
        {
            var clusterStatus = clusterSuccess ? "Success" : "Failed";

            DiagnosticsETWProvider.Instance.LogKustoHeartbeatInformation(
                activityId,
                $"ClusterType:{primaryOrFailover},ClusterStatus:{clusterStatus},Cluster:{cluster},UsingPrimaryCluster:{usingPrimaryCluster},SequenceNumber:{sequenceNumber}",
                exception != null ? exception.GetType().ToString() : string.Empty,
                exception != null ? exception.ToString() : string.Empty);
        }
    }
}