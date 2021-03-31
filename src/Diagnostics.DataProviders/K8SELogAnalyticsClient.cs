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
using Microsoft.Azure.OperationalInsights.Models;
using Diagnostics.Logger;

namespace Diagnostics.DataProviders
{
    class K8SELogAnalyticsClient : IK8SELogAnalyticsClient
    {
        private Microsoft.Rest.ServiceClientCredentials creds;
        private string workspaceId;
        private static OperationalInsightsDataClient client;
        private string _requestId;

        public K8SELogAnalyticsClient(string RequestId)
        {
            creds = K8SELogAnalyticsTokenService.Instance.getCreds();
            workspaceId = K8SELogAnalyticsTokenService.Instance.getWorkspaceId();
            client = new OperationalInsightsDataClient(creds);
            client.WorkspaceId = workspaceId;
            _requestId = RequestId;
        }

        public async Task<DataTable> ExecuteQueryAsync(string query)
        {
            var timeTakenStopWatch = new Stopwatch();
            QueryResults queryResults;
            Exception dataProviderException = null;
            //check query

            try
            {
                timeTakenStopWatch.Start();
                queryResults = await client.QueryAsync(query);
            }
            catch (Exception ex)
            {
                dataProviderException = ex;
                throw;
            }
            finally
            {
                timeTakenStopWatch.Stop();
                var StopTime = DateTime.Now;
                var StartTime = StopTime.AddMilliseconds(-timeTakenStopWatch.ElapsedMilliseconds);

                if (dataProviderException != null)
                {
                    DiagnosticsETWProvider.Instance.LogDataProviderException(_requestId, 
                        "K8SELogAnalyticsDataProvider", StartTime.ToString("HH:mm:ss.fff"),
                        StopTime.ToString("HH:mm:ss.fff"), timeTakenStopWatch.ElapsedMilliseconds,
                        dataProviderException.GetType().ToString(), dataProviderException.ToString());
                }
                else
                {
                    DiagnosticsETWProvider.Instance.LogDataProviderOperationSummary(_requestId,
                        "K8SELogAnalyticsDataProvider", StartTime.ToString("HH:mm:ss.fff"),
                        StopTime.ToString("HH:mm:ss.fff"), timeTakenStopWatch.ElapsedMilliseconds);
                }

            }

            if (queryResults == null)
            {
                queryResults = new QueryResults();
            }
            var dataTable = ResultAsDataTable(queryResults);
            return dataTable;
        }

        //convert results to DataTable
        private DataTable ResultAsDataTable(QueryResults results)
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

