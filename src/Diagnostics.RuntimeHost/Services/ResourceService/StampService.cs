using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils;
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
        Task<List<string>> GetTenantIdForStamp(string stamp, DateTime startTime, DateTime endTime, string requestId = null);
    }

    public class StampService : IStampService
    {
        private ConcurrentDictionary<string, List<string>> _tenantCache;
        private IDataSourcesConfigurationService _dataSourcesConfigService;
        private DataProviders.DataProviders _dataProviders; 

        public StampService(IDataSourcesConfigurationService dataSourcesConfigService)
        {
            _dataSourcesConfigService = dataSourcesConfigService;
            _tenantCache = new ConcurrentDictionary<string, List<string>>();
            _dataProviders = new DataProviders.DataProviders(_dataSourcesConfigService.Config);
        }

        public async Task<List<string>> GetTenantIdForStamp(string stamp, DateTime startTime, DateTime endTime, string requestId = null)
        {
            if (string.IsNullOrWhiteSpace(stamp))
            {
                throw new ArgumentNullException("stamp");
            }
            
            if (_tenantCache.TryGetValue(stamp.ToLower(), out List<string> tenantIds))
            {
                return tenantIds;
            }

            tenantIds = new List<string>();
            string startTimeStr = DateTimeHelper.GetDateTimeInUtcFormat(startTime).ToString(HostConstants.KustoTimeFormat);
            string endTimeStr = DateTimeHelper.GetDateTimeInUtcFormat(endTime).ToString(HostConstants.KustoTimeFormat);

            string query =
                $@"RoleInstanceHeartbeat
                | where TIMESTAMP >= datetime({startTimeStr}) and TIMESTAMP <= datetime({endTimeStr}) and PublicHost startswith ""{stamp}""
                | summarize by Tenant, PublicHost";
            
            DataTable tenantIdTable = await _dataProviders.Kusto.ExecuteQuery(query, stamp, requestId, "GetTenantIdForStamp");

            if (tenantIdTable != null && tenantIdTable.Rows.Count > 0)
            {
                foreach (DataRow row in tenantIdTable.Rows)
                {
                    tenantIds.Add(row["Tenant"].ToString());
                }

                _tenantCache.TryAdd(stamp, tenantIds);
            }

            return tenantIds;
        }
    }
}
