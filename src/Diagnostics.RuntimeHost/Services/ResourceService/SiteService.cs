using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.RuntimeHost.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services
{
    public interface ISiteService
    {
        Task<StackType> GetApplicationStack(string subscriptionId, string resourceGroup, string siteName, DataProviderContext dataProviderContext);
    }

    public class SiteService : ISiteService
    {
        public SiteService(IDataSourcesConfigurationService dataSourcesConfigService){ }

        public async Task<StackType> GetApplicationStack(string subscriptionId, string resourceGroup, string siteName, DataProviderContext dataProviderContext)
        {
            var dp = new DataProviders.DataProviders(dataProviderContext);
            if (string.IsNullOrWhiteSpace(subscriptionId)) throw new ArgumentNullException("subscriptionId");
            if (string.IsNullOrWhiteSpace(resourceGroup)) throw new ArgumentNullException("resourceGroup");
            if (string.IsNullOrWhiteSpace(siteName)) throw new ArgumentNullException("siteName");

            string queryTemplate =
                $@"WawsAn_dailyentity 
                | where pdate >= ago(5d) and sitename =~ ""{siteName}"" and sitesubscription =~ ""{subscriptionId}"" and resourcegroup =~ ""{resourceGroup}"" 
                | where sitestack !contains ""unknown"" and sitestack !contains ""no traffic"" and sitestack  !contains ""undefined""
                | top 1 by pdate desc
                | project sitestack";

            DataTable stackTable = await dp.Kusto.ExecuteQuery(queryTemplate, DataProviderConstants.FakeStampForAnalyticsCluster, operationName: "GetApplicationStack");
            
            if(stackTable == null || stackTable.Rows == null || stackTable.Rows.Count == 0)
            {
                return StackType.None;
            }

            return GetAppStackType(stackTable.Rows[0][0].ToString().ToLower());
        }

        private StackType GetAppStackType(string stackString)
        {
            switch (stackString)
            {
                case "asp.net":
                case "classic asp":
                case "aspnet":
                    return StackType.AspNet;
                case "asp.net core":
                case "dotnetcore":
                case @"dotnetcore""":
                    return StackType.NetCore;
                case "php":
                    return StackType.Php;
                case "python":
                    return StackType.Python;
                case "java":
                    return StackType.Java;
                case "node":
                    return StackType.Node;
                case "static only":
                case "static":
                    return StackType.Static;
                default:
                    return StackType.Other;
            }
        }
    }
}
