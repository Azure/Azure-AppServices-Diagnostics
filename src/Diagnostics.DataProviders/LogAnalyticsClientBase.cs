using Diagnostics.DataProviders.Interfaces;
using Diagnostics.Logger;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Azure.OperationalInsights.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using static Diagnostics.DataProviders.LogAnalyticsDataProvider;

namespace Diagnostics.DataProviders
{
    public abstract class LogAnalyticsClientBase : ILogAnalyticsClient
    {
        public abstract OperationalInsightsDataClient client { get; set; }
        public abstract string _requestId { get; set; }
        public abstract string dataProviderName { get; set; }

        public async Task<DataTable> ExecuteQueryAsync(string query)
        {
            var timeTakenStopWatch = new Stopwatch();
            QueryResults queryResults;
            Exception dataProviderException = null;

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
                        dataProviderName, StartTime.ToString("HH:mm:ss.fff"),
                        StopTime.ToString("HH:mm:ss.fff"), timeTakenStopWatch.ElapsedMilliseconds,
                        dataProviderException.GetType().ToString(), dataProviderException.ToString());
                }
                else
                {
                    DiagnosticsETWProvider.Instance.LogDataProviderOperationSummary(_requestId,
                        dataProviderName, StartTime.ToString("HH:mm:ss.fff"),
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

        public LogAnalyticsQuery GetLogAnalyticsQuery(string query)
        {
            var logAnalyticsQuery = new LogAnalyticsQuery
            {
                Text = query
            };

            return logAnalyticsQuery;
        }

        //convert results to DataTable
        private DataTable ResultAsDataTable(QueryResults results)
        {
            DataTable dataTable = new DataTable("results");
            dataTable.Clear();

            dataTable.Columns.AddRange(results.Tables[0].Columns.Select(s => new DataColumn(s.Name)).ToArray());
            var rows = results.Tables[0].Rows.Select(s => dataTable.NewRow().ItemArray = s.ToArray());
            foreach (var i in rows) { dataTable.Rows.Add(i); }

            return dataTable;
        }
    }
}
