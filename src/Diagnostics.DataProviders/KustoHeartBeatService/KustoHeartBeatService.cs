using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Diagnostics.Logger;

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
            foreach (string value in _configuration.RegionSpecificClusterNameCollection.Values)
            {
                // anything with the suffix follower has a failover to the cluster minus the suffix
                // TODO specifying the failover cluster should be done in the config file.
                if (value.EndsWith("follower"))
                {
                    if (!_heartbeats.ContainsKey(value))
                    {
                        string failoverCluster = value.Substring(0, value.Length - "follower".Length);
                        _heartbeats[value] = new KustoHeartBeat(value, failoverCluster, _kustoDataProvider);
                        // Start threads for each heartbeat 
                        ThreadStart threadStart = new ThreadStart(_heartbeats[value].RunHeartBeatThread);
                        Thread thread = new Thread(threadStart);
                        thread.Start();
                        _threads.Add(thread);
                    }
                }
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
                try
                {
                    // Run kusto query with  10 second time out
                    string query = "RoleInstanceHeartbeat | where TIMESTAMP >= ago(30m) | take 1";
                    var result = await _kustoDataProvider.ExecuteQueryDirect(query, PrimaryCluster, 10);

                    if (result.Rows.Count == 1)
                    {
                        heartBeatSuccess = true;
                    }
                }
                catch
                {
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

                Thread.Sleep(TimeSpan.FromSeconds(20));
            }
        }

    }
}