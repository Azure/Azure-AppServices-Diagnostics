using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.RuntimeHost.Utilities;

namespace Diagnostics.RuntimeHost.Services
{
    /// <summary>
    /// Interface for Stamp Service.
    /// </summary>
    public interface IStampService
    {
        Task<Tuple<List<string>, PlatformType>> GetTenantIdForStamp(string stamp, bool isPublicStamp, DateTime startTime, DateTime endTime, DataProviderContext dataProviderContext);
    }

    public class StampService : IStampService
    {
        private ConcurrentDictionary<string, Tuple<List<string>, PlatformType>> _tenantCache;
        protected const string RoleInstanceHeartBeatTableName = "RoleInstanceHeartbeat";
        protected const string LinuxRoleInstanceHeartBeatTableName = "LinuxRoleInstanceHeartBeats";

        public StampService()
        {
            _tenantCache = new ConcurrentDictionary<string, Tuple<List<string>, PlatformType>>();
        }

        public async Task<Tuple<List<string>, PlatformType>> GetTenantIdForStamp(string stamp, bool isPublicStamp, DateTime startTime, DateTime endTime, DataProviderContext dataProviderContext)
        {
            if (string.IsNullOrWhiteSpace(stamp))
            {
                throw new ArgumentNullException(nameof(stamp));
            }

            var dp = new DataProviders.DataProviders(dataProviderContext);

            if (_tenantCache.TryGetValue(stamp.ToLower(), out Tuple<List<string>, PlatformType> result))
            {
                return result;
            }

            var windowsTenantIdsTask = GetTenantIdsAsync(stamp, startTime, endTime, dataProviderContext, PlatformType.Windows);
            var linuxTenantIdsTask = GetTenantIdsAsync(stamp, startTime, endTime, dataProviderContext, PlatformType.Linux);
            var windowsTenantIds = await windowsTenantIdsTask;
            var linuxTenantIds = await linuxTenantIdsTask;

            PlatformType type = PlatformType.Windows;
            List<string> tenantIds = windowsTenantIds.Union(linuxTenantIds).ToList();

            if (windowsTenantIds.Any() && linuxTenantIds.Any())
            {
                type = PlatformType.Windows | PlatformType.Linux;
            }
            else if (linuxTenantIds.Any())
            {
                type = PlatformType.Linux;
            }

            result = new Tuple<List<string>, PlatformType>(tenantIds, type);

            // Only cache TenantIds if not empty and for Public Stamps
            if (tenantIds != null && tenantIds.Any() && isPublicStamp)
            {
                _tenantCache.TryAdd(stamp.ToLower(), result);
            }

            return result;
        }

        protected virtual async Task<List<string>> GetTenantIdsAsync(string stamp, DateTime startTime, DateTime endTime, DataProviderContext dataProviderContext, PlatformType platformType)
        {
            var dp = new DataProviders.DataProviders(dataProviderContext);
            var tenantIds = new List<string>();
            var kustoQuery = GetTenantIdQuery(stamp, startTime, endTime, platformType);
            var kustoTask = dp.Kusto.ExecuteQuery(kustoQuery, stamp, operationName: platformType == PlatformType.Windows ? KustoOperations.GetTenantIdForWindows : KustoOperations.GetTenantIdForLinux);
            tenantIds = GetTenantIdsFromTable(await kustoTask);
            return tenantIds;
        }

        private List<string> GetTenantIdsFromTable(DataTable dt)
        {
            List<string> tenantIds = new List<string>();

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    tenantIds.Add(row["Tenant"].ToString());
                }
            }

            return tenantIds;
        }

        private string GetTenantIdQuery(string stamp, DateTime startTime, DateTime endTime, PlatformType type)
        {
            string startTimeStr = DateTimeHelper.GetDateTimeInUtcFormat(startTime).ToString(DataProviderConstants.KustoTimeFormat);
            string endTimeStr = DateTimeHelper.GetDateTimeInUtcFormat(endTime).ToString(DataProviderConstants.KustoTimeFormat);
            string tableName = RoleInstanceHeartBeatTableName;
            if (type == PlatformType.Linux)
            {
                tableName = LinuxRoleInstanceHeartBeatTableName;
            }

            return GetTenantIdQuery(stamp, startTimeStr, endTimeStr, tableName);
        }

        protected virtual string GetTenantIdQuery(string stamp, string startTimeStr, string endTimeStr, string tableName)
        {
            return
                $@"{tableName}
                | where TIMESTAMP >= datetime({startTimeStr}) and TIMESTAMP <= datetime({endTimeStr})
                | where PublicHost =~ ""{stamp}.cloudapp.net"" or PublicHost matches regex ""{stamp}([a-z{{1}}]).cloudapp.net""
                | summarize by Tenant, PublicHost";
        }
    }
}
