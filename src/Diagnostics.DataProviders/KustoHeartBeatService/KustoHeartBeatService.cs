using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Diagnostics.Logger;
using Microsoft.AspNetCore.Http.Features;

namespace Diagnostics.DataProviders
{
    public interface IKustoHeartBeatService : IDisposable
    {
        string GetClusterNameFromStamp(string stampName);
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

            foreach (var value in _configuration.FailoverClusterNameCollection)
            {
                _heartbeats[value.Key] = new KustoHeartBeat(value.Key, value.Value, _kustoDataProvider, _configuration);
                // Start threads for each heartbeat on the thread pool
                Task.Run(() => _heartbeats[value.Key].RunHeartBeatTask(_cancellationToken.Token));
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

        public string GetClusterNameFromStamp(string stampName)
        {
            string kustoClusterName = null;
            string appserviceRegion = ParseRegionFromStamp(stampName);

            if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue(appserviceRegion.ToLower(), out kustoClusterName))
            {
                if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue("*", out kustoClusterName))
                {
                    throw new KeyNotFoundException(String.Format("Kusto Cluster Name not found for Region : {0}", appserviceRegion.ToLower()));
                }
            }

            if (_heartbeats.ContainsKey(kustoClusterName))
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

        public async Task RunHeartBeatTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested) {
                bool primaryHeartBeatSuccess = false;
                bool failoverHeartBeatSuccess = false;
                Exception exceptionForLog = null;
                string activityId = Guid.NewGuid().ToString();
                try
                {
                    // Run kusto query with time out
                    var primaryTask = _kustoDataProvider.ExecuteQueryForHeartbeat(_configuration.HeartBeatQuery, PrimaryCluster, _configuration.HeartBeatTimeOut, activityId);
                    var failoverTask = _kustoDataProvider.ExecuteQueryForHeartbeat(_configuration.HeartBeatQuery, FailoverCluster, _configuration.HeartBeatTimeOut, activityId);
                    await Task.WhenAll(new Task[] { primaryTask, failoverTask });

                    var result = await primaryTask;
                    if (result.Rows.Count >= 1)
                    {
                        primaryHeartBeatSuccess = true;
                    }

                    var failoverResult = await failoverTask;
                    if (failoverResult.Rows.Count >= 1)
                    {
                        failoverHeartBeatSuccess = true;
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
                if (UsePrimaryCluster && _ConsecutiveFailureCount >= 5)
                {
                    UsePrimaryCluster = false;
                } // else should stop failover
                else if (!UsePrimaryCluster && _ConsecutiveSuccessCount >= 5)
                {
                    UsePrimaryCluster = true;
                }

                LogHeartBeatInformation(primaryHeartBeatSuccess, failoverHeartBeatSuccess, PrimaryCluster, FailoverCluster, activityId, _ConsecutiveSuccessCount, _ConsecutiveFailureCount, exceptionForLog);

                await Task.Delay(TimeSpan.FromSeconds(_configuration.HeartBeatDelay), cancellationToken);
            }
        }

        private void LogHeartBeatInformation(bool primaryClustersuccess, bool failoverClusterSuccess, string cluster, string failoverCluster, string invocationId, int successCount, int failureCount, Exception exception)
        {
            var primaryClusterStatus = primaryClustersuccess ? "Success" : "Failed";
            var failoverClusterStatus = failoverClusterSuccess ? "Success" : "Failed";


            DiagnosticsETWProvider.Instance.LogKustoHeartbeatInformation(
               invocationId,
               $"PrimaryClusterStatus:{primaryClusterStatus},FailoverClusterStatus:{failoverClusterStatus},PrimaryCluster:{cluster},FailoverCluster:{failoverCluster},PrimaryClusterConsecutiveSuccessCount:{successCount},PrimaryClusterConsecutiveFailureCount:{failureCount}",
               exception != null ? exception.GetType().ToString() : string.Empty,
               exception != null ? exception.ToString() : string.Empty);
        }
    }
}