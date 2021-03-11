using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Kusto.Data;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    class K8SELogAnalyticsClient : IK8SELogAnalyticsClient
    {
        private K8SELogAnalyticsDataProviderConfiguration _configuration;

        public K8SELogAnalyticsClient(K8SELogAnalyticsDataProviderConfiguration configuration)
        {
            _configuration = configuration;
        }

        //convert results to DataTable
        private DataTable ResultAsDataTable(Microsoft.Azure.OperationalInsights.Models.QueryResults results)
        {
            DataTable dataTable = new DataTable("results");
            dataTable.Clear();

            int cursor = 0;
            foreach (var i in results.Tables[0].Columns)
            {
                dataTable.Columns.Add(results.Tables[0].Columns[cursor++].Name);
            }

            int iCursor = 0;
            int jCursor;
            foreach (var i in results.Tables[0].Rows)
            {
                DataRow row = dataTable.NewRow();

                jCursor = 0;
                foreach (var j in results.Tables[0].Columns)
                {
                    row[results.Tables[0].Columns[jCursor].Name] = results.Tables[0].Rows[iCursor][jCursor++];
                }
                iCursor++;

                dataTable.Rows.Add(row);

            }

            return dataTable;
        }

        public Task<DataTable> ExecuteQueryAsync(string query)
        {
            var workspaceId = "ac5995da-1be5-43b8-942f-64823ec16600";
            var clientId = "12a0e393-3c1c-4aa2-ac43-2a2ad31da45a";
            var clientSecret = "~j~9K1U2-cppnOVD..~.5y.FlyB6QJ7yv7";

            var domain = "microsoft.onmicrosoft.com";
            var authEndpoint = "https://login.microsoftonline.com";
            var tokenAudience = "https://api.loganalytics.io/";

            var adSettings = new ActiveDirectoryServiceSettings
            {
                AuthenticationEndpoint = new Uri(authEndpoint),
                TokenAudience = new Uri(tokenAudience),
                ValidateAuthority = true
            };

            var creds = ApplicationTokenProvider.LoginSilentAsync(domain, clientId, clientSecret, adSettings).GetAwaiter().GetResult();

            var client = new OperationalInsightsDataClient(creds);
            client.WorkspaceId = workspaceId;

            var queryResults = client.Query(query);

            var dataTable = ResultAsDataTable(queryResults);

            Task<DataTable> task = Task.FromResult<DataTable>(dataTable);

            return task;
        }
    }
}

