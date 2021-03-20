using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Kusto.Data;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders.TokenService;
using System.Diagnostics;

namespace Diagnostics.DataProviders
{
    class K8SELogAnalyticsClient : IK8SELogAnalyticsClient
    {
        private K8SELogAnalyticsDataProviderConfiguration _configuration;

        public K8SELogAnalyticsClient(K8SELogAnalyticsDataProviderConfiguration configuration)
        {
            _configuration = configuration;
            K8SELogAnalyticsTokenService.Instance.Initialize(_configuration);
        }

        public async Task<DataTable> ExecuteQueryAsync(string query)
        {
            var timeTakenStopWatch = new Stopwatch();
            Microsoft.Azure.OperationalInsights.Models.QueryResults queryResults;


            try
            {
                timeTakenStopWatch.Start();
                queryResults = await K8SELogAnalyticsTokenService.Instance.GetClient().QueryAsync(query);
            }
            catch (Exception ex)
            {
                timeTakenStopWatch.Stop();
                throw;
            }
            finally
            {
                timeTakenStopWatch.Stop();
            }

            if (queryResults == null)
            {
                queryResults = new Microsoft.Azure.OperationalInsights.Models.QueryResults();
            }
            var dataTable = ResultAsDataTable(queryResults);
            return dataTable;
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
    }
}

