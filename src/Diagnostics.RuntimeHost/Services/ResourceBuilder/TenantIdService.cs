using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils;
using Diagnostics.RuntimeHost.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services
{
    public interface ITenantIdService
    {
        Task<List<string>> GetTenantIdForStamp(string stamp, DateTime startTime, DateTime endTime, string requestId = null);
    }

    public class TenantIdService : ITenantIdService
    {
        private ConcurrentDictionary<string, List<string>> _tenantCache;
        private string _queryTemplate;
        private IDataSourcesConfigurationService _dataSourcesConfigService;
        private DataProviders.DataProviders _dataProviders; 

        public TenantIdService(IDataSourcesConfigurationService dataSourcesConfigService)
        {
            _dataSourcesConfigService = dataSourcesConfigService;
            _tenantCache = new ConcurrentDictionary<string, List<string>>();
            _queryTemplate =
            @"RoleInstanceHeartbeat
              | where TIMESTAMP >= datetime({StartTime}) and TIMESTAMP <= datetime({EndTime}) and PublicHost startswith ""{StampName}""
              | summarize by Tenant, PublicHost";

            _dataProviders = new DataProviders.DataProviders(_dataSourcesConfigService.Config);
        }

        public async Task<List<string>> GetTenantIdForStamp(string stamp, DateTime startTime, DateTime endTime, string requestId = null)
        {
            if (string.IsNullOrWhiteSpace(stamp))
            {
                throw new ArgumentNullException("stamp cannot be null.");
            }

            if (_tenantCache.TryGetValue(stamp.ToLower(), out List<string> tenantIds))
            {
                return tenantIds;
            }

            tenantIds = new List<string>();
            string startTimeStr = DateTimeHelper.GetDateTimeInUtcFormat(startTime).ToString(HostConstants.KustoTimeFormat);
            string endTimeStr = DateTimeHelper.GetDateTimeInUtcFormat(endTime).ToString(HostConstants.KustoTimeFormat);

            var query = _queryTemplate
                .Replace("{StartTime}", startTimeStr)
                .Replace("{EndTime}", endTimeStr)
                .Replace("{StampName}", stamp);
            
            DataTableResponseObject response = await _dataProviders.Kusto.ExecuteQuery(query, stamp, requestId, "GetTenantIdForStamp");
            DataTable tenantIdTable = DataTableUtility.GetDataTable(response);

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
