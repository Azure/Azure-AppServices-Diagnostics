namespace Diagnostics.RuntimeHost.Utilities
{
    internal class HostConstants
    {
        internal const int WatcherDefaultPollingIntervalInSeconds = 5 * 60;
        internal const string ApiLoggerKey = "API_LOGGER";
        internal const string DataProviderContextKey = "DATA_PROVIDER_CONTEXT";
        internal const int TimeoutInMilliSeconds = 60 * 1000;
        internal const string AzureCloud = "PublicAzure";
        internal const string AzureChinaCloud = "China";
        internal const string AzureUSGovernment = "Government";
        internal const int KustoDelayInSeconds = 15;
    }

    internal class RegistryConstants
    {
        internal const string RegistryRootPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\IIS Extensions\Web Hosting Framework";

        // Compiler Host Registry Settings
        internal const string CompilerHostRegistryPath = RegistryRootPath + @"\CompilerHost";

        internal const string CompilerHostBinaryLocationKey = "CompilerHostBinaryLocation";
        internal const string CompilerHostUrlKey = "CompilerHostUrl";
        internal const string CompilerHostPollingIntervalKey = "PollingIntervalInSeconds";
        internal const string CompilerHostProcessMemoryThresholdInMBKey = "ProcessMemoryThresholdInMB";

        // Source Watcher Registry Settings
        internal const string SourceWatcherRegistryPath = RegistryRootPath + @"\SourceWatcher";

        internal const string WatcherTypeKey = "WatcherType";
        internal const string PollingIntervalInSecondsKey = "PollingIntervalInSeconds";
        internal const string LoadOnlyPublicDetectorsKey = "LoadOnlyPublicDetectors";
        internal const string LocalWatcherRegistryPath = SourceWatcherRegistryPath + @"\Local";
        internal const string LocalScriptsPathKey = "LocalScriptsPath";
        internal const string GithubWatcherRegistryPath = SourceWatcherRegistryPath + @"\Github";
        internal const string GithubAccessTokenKey = "AccessToken";
        internal const string GithubUserNameKey = "UserName";
        internal const string GithubRepoNameKey = "RepoName";
        internal const string GithubBranchKey = "Branch";
        internal const string DestinationScriptsPathKey = "DestinationScriptsPath";

        // Search API Registry Settings
        internal const string SearchAPIRegistryPath = RegistryRootPath + @"\SearchAPI";
        internal const string SearchAPIEnabledKey = "SearchAPIEnabled";
    }
}
