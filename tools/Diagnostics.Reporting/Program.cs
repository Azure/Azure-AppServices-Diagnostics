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

            //OverallMetrics.Run(ks, Configuration);
            var linux = new LinuxAppMetrics();
            linux.Run(ks, Configuration);
        }

        private static KustoDataProviderConfiguration PrepareKustoConfig()
        {
            var config = new KustoDataProviderConfiguration()
            {
                AppKey = Configuration["Kusto:AppKey"].ToString(),
                ClientId = Configuration["Kusto:ClientId"].ToString(),
                DBName = Configuration["Kusto:DBName"].ToString(),
                KustoClusterNameGroupings = Configuration["Kusto:KustoRegionGroupings"].ToString(),
                KustoRegionGroupings = Configuration["Kusto:KustoClusterNameGroupings"].ToString()
            };

            config.RegionSpecificClusterNameCollection = new ConcurrentDictionary<string, string>();
            config.RegionSpecificClusterNameCollection.TryAdd("blu", "wawseus");

            return config;
        }
    }
}
