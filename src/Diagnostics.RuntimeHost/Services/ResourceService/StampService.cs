using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IStampService
    {
        Task<Tuple<List<string>, PlatformType>> GetTenantIdForStamp(string stamp, bool isPublicStamp, DateTime startTime, DateTime endTime, DataProviderContext dataProviderContext);
    }

    public class StampService : IStampService
    {
        private ConcurrentDictionary<string, Tuple<List<string>, PlatformType>> _tenantCache;

        public StampService()
        {
            _tenantCache = new ConcurrentDictionary<string, Tuple<List<string>, PlatformType>>();
        }

        public async Task<Tuple<List<string>, PlatformType>> GetTenantIdForStamp(string stamp, bool isPublicStamp, DateTime startTime, DateTime endTime, DataProviderContext dataProviderContext)
        {
            var dp = new DataProviders.DataProviders(dataProviderContext);
            if (string.IsNullOrWhiteSpace(stamp))
            {
                throw new ArgumentNullException("stamp");
            }

            if (_tenantCache.TryGetValue(stamp.ToLower(), out Tuple<List<string>, PlatformType> result))
            {
                return result;
            }

            List<string> windowsTenantIds = new List<string>();
            List<string> linuxTenantIds = new List<string>();

            string windowsQuery = GetTenantIdQuery(stamp, startTime, endTime, PlatformType.Windows);
            string linuxQuery = GetTenantIdQuery(stamp, startTime, endTime, PlatformType.Linux);

            var windowsTask = dp.Kusto.ExecuteQuery(windowsQuery, stamp, operationName: KustoOperations.GetTenantIdForWindows);
            var linuxTask = dp.Kusto.ExecuteQuery(linuxQuery, stamp, operationName: KustoOperations.GetTenantIdForLinux);

            windowsTenantIds = GetTenantIdsFromTable(await windowsTask);
            linuxTenantIds = GetTenantIdsFromTable(await linuxTask);

            PlatformType type = PlatformType.Windows;
            List<string> tenantIds = windowsTenantIds.Union(linuxTenantIds).ToList();

            if (windowsTenantIds.Any() && linuxTenantIds.Any()) type = PlatformType.Windows | PlatformType.Linux;
            else if (linuxTenantIds.Any()) type = PlatformType.Linux;
            
            result = new Tuple<List<string>, PlatformType>(tenantIds, type);

            // Only cache TenantIds for Public stamps
            if (isPublicStamp)
            {
                _tenantCache.TryAdd(stamp.ToLower(), result);
            }           

            return result;
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
            string tableName = "RoleInstanceHeartbeat";
            if (type == PlatformType.Linux)
            {
                tableName = "LinuxRoleInstanceHeartBeats";
            }

            return
                $@"{tableName}
                | where TIMESTAMP >= datetime({startTimeStr}) and TIMESTAMP <= datetime({endTimeStr}) 
                | where PublicHost =~ ""{stamp}.cloudapp.net"" or PublicHost matches regex ""{stamp}([a-z{{1}}]).cloudapp.net""
                | summarize by Tenant, PublicHost";
        }
    }
}
