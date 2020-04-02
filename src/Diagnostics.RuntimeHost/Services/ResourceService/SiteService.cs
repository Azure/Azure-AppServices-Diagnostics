using System;
using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Attributes;

namespace Diagnostics.RuntimeHost.Services
{
    public interface ISiteService
    {
        Task<string> GetApplicationStack(string subscriptionId, string resourceGroup, string siteName, DataProviderContext dataProviderContext);
    }

    public class SiteService : ISiteService
    {
        public async Task<string> GetApplicationStack(string subscriptionId, string resourceGroup, string siteName, DataProviderContext dataProviderContext)
        {
            var dp = new DataProviders.DataProviders(dataProviderContext);
            if (string.IsNullOrWhiteSpace(subscriptionId)) throw new ArgumentNullException("subscriptionId");
            if (string.IsNullOrWhiteSpace(resourceGroup)) throw new ArgumentNullException("resourceGroup");
            if (string.IsNullOrWhiteSpace(siteName)) throw new ArgumentNullException("siteName");

            string queryTemplate =
                $@"set query_results_cache_max_age = time(1d);
                WawsAn_dailyentity
                | where pdate >= ago(5d) and sitename =~ ""{siteName}"" and sitesubscription =~ ""{subscriptionId}"" and resourcegroup =~ ""{resourceGroup}""
                | where sitestack !contains ""unknown"" and sitestack !contains ""no traffic"" and sitestack  !contains ""undefined""
                | top 1 by pdate desc
                | project sitestack";

            DataTable stackTable = null;

            try
            {
                if (dataProviderContext.Configuration.KustoConfiguration.CloudDomain == DataProviderConstants.AzureCloud)
                {
                    stackTable = await dp.Kusto.ExecuteQuery(queryTemplate, DataProviderConstants.FakeStampForAnalyticsCluster, operationName: "GetApplicationStack");
                }
            }
            catch (Exception ex)
            {
                //swallow the exception. Since Mooncake does not have an analytics cluster
                DiagnosticsETWProvider.Instance.LogRuntimeHostHandledException(dataProviderContext.RequestId, "GetApplicationStack", subscriptionId,
                    resourceGroup, siteName, ex.GetType().ToString(), ex.ToString());
            }

            if (stackTable == null || stackTable.Rows == null || stackTable.Rows.Count == 0)
            {
                return "Unknown";
            }

            return stackTable.Rows[0][0].ToString();
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
