using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils.Attributes;

namespace Diagnostics.RuntimeHost.Services
{
    public sealed class NationalCloudStampService : StampService
    {
        protected override async Task<List<string>> GetTenantIdsAsync(string stamp, DateTime startTime, DateTime endTime, DataProviderContext dataProviderContext, PlatformType platformType)
        {
            if (platformType == PlatformType.Windows || platformType == PlatformType.HyperV)
            {
                return await base.GetTenantIdsAsync(stamp, startTime, endTime, dataProviderContext, platformType);
            }
            else
            {
                return await Task.FromResult(new List<string>());
            }
        }

        protected override string GetTenantIdQuery(string stamp, string startTimeStr, string endTimeStr, string tableName)
        {
            return
                $@"{RoleInstanceHeartBeatTableName}
                | where TIMESTAMP >= datetime({startTimeStr}) and TIMESTAMP <= datetime({endTimeStr}) and EventStampName =~ ""{stamp}""
                | summarize by Tenant, PublicHost";
        }
    }
}
