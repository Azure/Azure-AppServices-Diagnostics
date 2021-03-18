using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Kusto.Data;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders.TokenService;

namespace Diagnostics.DataProviders
{
    class K8SELogAnalyticsClient : IK8SELogAnalyticsClient
    {
        private K8SELogAnalyticsDataProviderConfiguration _configuration;

        private K8SETokenService _tokenService;

        /*private string workspaceId;
        private string clientId;
        private string clientSecret;

        private string domain;
        private string authEndpoint;
        private string tokenAudience;

        private ActiveDirectoryServiceSettings adSettings;
        private Microsoft.Rest.ServiceClientCredentials creds;*/

        public K8SELogAnalyticsClient(K8SELogAnalyticsDataProviderConfiguration configuration)
        {
            _configuration = configuration;
            K8SETokenService.Instance.Initialize(_configuration);

            /*workspaceId = _configuration.WorkspaceId;
            clientId = _configuration.ClientId;
            clientSecret = _configuration.ClientSecret;

            domain = _configuration.Domain;
            authEndpoint = _configuration.AuthEndpoint;
            tokenAudience = _configuration.TokenAudience;*/

        }

        public async Task<DataTable> ExecuteQueryAsync(string query)
        {
            /*creds = await ApplicationTokenProvider.LoginSilentAsync(domain, clientId, clientSecret, adSettings);

            adSettings = new ActiveDirectoryServiceSettings
            {
                AuthenticationEndpoint = new Uri(authEndpoint),
                TokenAudience = new Uri(tokenAudience),
                ValidateAuthority = true
            };*/
            /*var client = new OperationalInsightsDataClient(_tokenService.GetCreds());
            client.WorkspaceId = _tokenService.GetWorkspaceId();*/

            var queryResults = await K8SETokenService.Instance.GetClient().QueryAsync(query);

            var dataTable = ResultAsDataTable(queryResults);

            return dataTable;
        }

        //assign creds asynchronously
        /*private async Task GetCredsAsync()
        {
            creds = await ApplicationTokenProvider.LoginSilentAsync(domain, clientId, clientSecret, adSettings);
        }*/

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
    }
}

