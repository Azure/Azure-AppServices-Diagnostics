using Diagnostics.DataProviders;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Diagnostics.Reporting
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            KustoDataProviderConfiguration config = PrepareKustoConfig();
            KustoTokenService.Instance.Initialize(config);
            KustoClient ks = new KustoClient(config);

            P360TableResolver.Init(ks);

            //OverallMetrics.Run(ks, Configuration);
            //ProductLevelMetrics.Run(ks, Configuration);
            CategoryLevelMetrics.Run(ks, Configuration);
        }

        private static KustoDataProviderConfiguration PrepareKustoConfig()
        {
            var config = new KustoDataProviderConfiguration()
            {
                AppKey = Configuration["Kusto:AppKey"].ToString(),
                ClientId = Configuration["Kusto:ClientId"].ToString(),
                DBName = Configuration["Kusto:DBName"].ToString(),
                KustoClusterNameGroupings = string.Empty,
                KustoRegionGroupings = string.Empty
            };

            config.RegionSpecificClusterNameCollection = new ConcurrentDictionary<string, string>();
            config.RegionSpecificClusterNameCollection.TryAdd("*", Configuration["Kusto:ClusterName"].ToString());

            return config;
        }
    }
}
