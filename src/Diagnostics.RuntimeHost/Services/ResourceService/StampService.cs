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
        Task<Tuple<List<string>, PlatformType>> GetTenantIdForStamp(string stamp, DateTime startTime, DateTime endTime, string requestId = null);
    }

    public class StampService : IStampService
    {
        private ConcurrentDictionary<string, Tuple<List<string>, PlatformType>> _tenantCache;
        private IDataSourcesConfigurationService _dataSourcesConfigService;
        private DataProviders.DataProviders _dataProviders; 

        public StampService(IDataSourcesConfigurationService dataSourcesConfigService)
        {
            _dataSourcesConfigService = dataSourcesConfigService;
            _tenantCache = new ConcurrentDictionary<string, Tuple<List<string>, PlatformType>>();
            _dataProviders = new DataProviders.DataProviders(_dataSourcesConfigService.Config);
        }

        public async Task<Tuple<List<string>, PlatformType>> GetTenantIdForStamp(string stamp, DateTime startTime, DateTime endTime, string requestId = null)
        {
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

            var windowsTask = _dataProviders.Kusto.ExecuteQuery(windowsQuery, stamp, requestId, "GetTenantIdForStamp-Windows");
            var linuxTask = _dataProviders.Kusto.ExecuteQuery(linuxQuery, stamp, requestId, "GetTenantIdForStamp-Linux");

            windowsTenantIds = GetTenantIdsFromTable(await windowsTask);
            linuxTenantIds = GetTenantIdsFromTable(await linuxTask);

            PlatformType type = PlatformType.Windows;
            List<string> tenantIds = windowsTenantIds.Union(linuxTenantIds).ToList();

            if (windowsTenantIds.Any() && linuxTenantIds.Any()) type = PlatformType.Windows | PlatformType.Linux;
            else if (linuxTenantIds.Any()) type = PlatformType.Linux;
            
            result = new Tuple<List<string>, PlatformType>(tenantIds, type);

            _tenantCache.TryAdd(stamp.ToLower(), result);
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
            string startTimeStr = DateTimeHelper.GetDateTimeInUtcFormat(startTime).ToString(HostConstants.KustoTimeFormat);
            string endTimeStr = DateTimeHelper.GetDateTimeInUtcFormat(endTime).ToString(HostConstants.KustoTimeFormat);
            string tableName = "RoleInstanceHeartbeat";
            if (type == PlatformType.Linux)
            {
                tableName = "LinuxRoleInstanceHeartBeats";
            }

            return
                $@"{tableName}
                | where TIMESTAMP >= datetime({startTimeStr}) and TIMESTAMP <= datetime({endTimeStr}) and PublicHost startswith ""{stamp}""
                | summarize by Tenant, PublicHost";
        }
    }
}
