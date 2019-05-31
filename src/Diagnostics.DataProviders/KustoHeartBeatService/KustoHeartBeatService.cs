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
        Dictionary<string, KustoHeartBeat> _heartbeats;
        KustoDataProvider _kustoDataProvider;

        List<Thread> _threads;

        private void Initialize()
        {
            _heartbeats = new Dictionary<string, KustoHeartBeat>();
            _kustoDataProvider = new KustoDataProvider(new OperationDataCache(), _configuration, Guid.NewGuid().ToString(), this);
            _threads = new List<Thread>();
            foreach (var value in _configuration.FailoverClusterNameCollection)
            {
                _heartbeats[value.Key] = new KustoHeartBeat(value.Key, value.Value, _kustoDataProvider);
                // Start threads for each heartbeat
                ThreadStart threadStart = new ThreadStart(_heartbeats[value.Key].RunHeartBeatThread);
                Thread thread = new Thread(threadStart);
                thread.Start();
                _threads.Add(thread);
            }
        }


        public void Dispose()
        {
            foreach (var thread in _threads)
            {
                thread.Abort();
            }
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

        public KustoHeartBeat(string primaryCluster, string failoverCluster, KustoDataProvider kustoDataProvider)
        {
            PrimaryCluster = primaryCluster;
            FailoverCluster = failoverCluster;
            UsePrimaryCluster = true;
            _kustoDataProvider = kustoDataProvider;
        }

        public async void RunHeartBeatThread()
        {
            while (true) {
                bool heartBeatSuccess = false;
                Exception exceptionForLog = null;
                string requestID = Guid.NewGuid().ToString();
                try
                {
                    // Run kusto query with  10 second time out
                    string query = "RoleInstanceHeartbeat | where TIMESTAMP >= ago(30m) | take 1";
                    var result = await _kustoDataProvider.ExecuteQueryDirect(query, PrimaryCluster, 10, requestID);

                    if (result.Rows.Count == 1)
                    {
                        heartBeatSuccess = true;
                    }
                }
                catch (Exception ex)
                {
                    exceptionForLog = ex;
                }

                if (heartBeatSuccess)
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

                LogHeartBeatInformation(heartBeatSuccess, PrimaryCluster, FailoverCluster, requestID, _ConsecutiveSuccessCount, _ConsecutiveFailureCount, exceptionForLog);

                Thread.Sleep(TimeSpan.FromSeconds(20));
            }
        }

        private void LogHeartBeatInformation(bool success, string cluster, string failoverCluster, string requestID, int successCount, int failureCount, Exception exception)
        {
            var status = success ? "Success" : "Failed";


            DiagnosticsETWProvider.Instance.LogKustoHeartbeatInformation(
               requestID,
               $"Status:{status},PriamryCluster:{cluster},FasiloverCluster:{failoverCluster},ConsecutiveSuccessCount:{successCount},ConsecutiveFailureCount:{failureCount}",
               exception != null ? exception.GetType().ToString() : string.Empty,
               exception != null ? exception.ToString() : string.Empty);
        }
    }
}