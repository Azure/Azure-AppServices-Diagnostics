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
