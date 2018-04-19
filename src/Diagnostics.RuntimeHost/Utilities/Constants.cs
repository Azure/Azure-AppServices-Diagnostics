using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Utilities
{
    internal class HostConstants
    {
        internal const int WatcherDefaultPollingIntervalInSeconds = 5 * 60;
        internal const string ApiLoggerKey = "API_LOGGER";

        // These Configurations probably should be in Data Providers

        public static TimeSpan KustoDataRetentionPeriod = TimeSpan.FromDays(-30);

        public static TimeSpan KustoDataLatencyPeriod = TimeSpan.FromMinutes(15);

        public const int DefaultTimeGrainInMinutes = 5;

        public const string KustoTimeFormat = "yyyy-MM-dd HH:mm:ss";

        public const string FakeStampForAnalyticsCluster = "waws-prod-blu-000";

        #region Time Grain Constants

        internal static List<Tuple<TimeSpan, TimeSpan, bool>> TimeGrainOptions = new List<Tuple<TimeSpan, TimeSpan, bool>>
            {
                // 5 minute grain - max time range 1 day
                new Tuple<TimeSpan, TimeSpan, bool>(TimeSpan.FromMinutes(5), TimeSpan.FromDays(1), true),

                // 30 minute grain - max time range 3 days
                new Tuple<TimeSpan, TimeSpan, bool>(TimeSpan.FromMinutes(30), TimeSpan.FromDays(3),  false),
                
                // 1 hour grain - max time range 7 days
                new Tuple<TimeSpan, TimeSpan, bool>(TimeSpan.FromHours(1), TimeSpan.FromDays(7), false),
            };

        #endregion
    }
    
    internal class RegistryConstants
    {
        internal const string RegistryRootPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\IIS Extensions\Web Hosting Framework";

        // Compiler Host Registry Settings
        internal const string CompilerHostRegistryPath = RegistryRootPath + @"\CompilerHost";
        internal const string CompilerHostBinaryLocationKey = "CompilerHostBinaryLocation";
        internal const string CompilerHostPortKey = "CompilerHostPort";
        internal const string CompilerHostPollingIntervalKey = "PollingIntervalInSeconds";
        internal const string CompilerHostProcessMemoryThresholdInMBKey = "ProcessMemoryThresholdInMB";

        // Source Watcher Registry Settings
        internal const string SourceWatcherRegistryPath = RegistryRootPath + @"\SourceWatcher";
        internal const string WatcherTypeKey = "WatcherType";
        internal const string PollingIntervalInSecondsKey = "PollingIntervalInSeconds";
        internal const string LocalWatcherRegistryPath = SourceWatcherRegistryPath + @"\Local";
        internal const string LocalScriptsPathKey = "LocalScriptsPath";
        internal const string GithubWatcherRegistryPath = SourceWatcherRegistryPath + @"\Github";
        internal const string GithubAccessTokenKey = "AccessToken";
        internal const string GithubUserNameKey = "UserName";
        internal const string GithubRepoNameKey = "RepoName";
        internal const string GithubBranchKey = "Branch";
        internal const string DestinationScriptsPathKey = "DestinationScriptsPath";
    }
}
