using System;
using System.Collections.Generic;
using Kusto.Data.Common;

namespace Diagnostics.DataProviders
{
    public class DataProviderConstants
    {
        public const int TokenRefreshIntervalInMs = 15 * 60 * 1000;

        public const int DefaultTimeoutInSeconds = 60;

        public const string AzureCloud = "PublicAzure";

        public const string AzureCloudAlternativeName = "Public";

        public const string AzureChinaCloud = "China";

        public const string AzureChinaCloudCodename = "Mooncake";

        public const string AzureUSGovernment = "Government";

        public const string AzureUSGovernmentCodename = "Fairfax";

        public const string AzureUSSec = "USSec";

        public const string AzureUSNat = "USNat";

        public static readonly char[] CommonSeparationChars = { '|', ',' };

        #region Kusto Constants

        public static TimeSpan KustoDataRetentionPeriod = TimeSpan.FromDays(-30);

        public static TimeSpan KustoDataLatencyPeriod = TimeSpan.FromMinutes(15);

        public const int DefaultTimeGrainInMinutes = 5;

        public const string KustoTimeFormat = "yyyy-MM-dd HH:mm:ss";

        public const string FakeStampForAnalyticsCluster = "waws-prod-blu-000";

        public const string kustoFollowerClusterSuffix = "follower";

        #endregion Kusto Constants

        #region Time Grain Constants

        public static List<Tuple<TimeSpan, TimeSpan, bool>> TimeGrainOptions = new List<Tuple<TimeSpan, TimeSpan, bool>>
            {
                // 5 minute grain - max time range 1 day
                new Tuple<TimeSpan, TimeSpan, bool>(TimeSpan.FromMinutes(5), TimeSpan.FromDays(1), true),

                // 30 minute grain - max time range 3 days
                new Tuple<TimeSpan, TimeSpan, bool>(TimeSpan.FromMinutes(30), TimeSpan.FromDays(3),  false),

                // 1 hour grain - max time range 7 days
                new Tuple<TimeSpan, TimeSpan, bool>(TimeSpan.FromHours(1), TimeSpan.FromDays(7), false),
            };

        #endregion Time Grain Constants
    }

    public class KustoOperations
    {
        public const string GetTenantIdForWindows = "gettenantidforstamp-windows";
        public const string GetTenantIdForLinux = "gettenantidforstamp-linux";
        public const string GetLatestDeployment = "getlatestdeployment";
    }
}
