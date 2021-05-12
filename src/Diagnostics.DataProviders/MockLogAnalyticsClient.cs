using Diagnostics.DataProviders.Interfaces;
using Microsoft.Azure.OperationalInsights.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Diagnostics.DataProviders.LogAnalyticsDataProvider;

namespace Diagnostics.DataProviders
{
    internal class MockLogAnalyticsClient : ILogAnalyticsClient
    {
        public async Task<DataTable> ExecuteQueryAsync(string query)
        {
            var queryResults = new QueryResults();
            var dataTable = ResultAsDataTable(queryResults);
            return dataTable;
        }

        public async Task<LogAnalyticsQuery> GetLogAnalyticsQueryAsync(string query)
        {
            var logAnalyticsQuery = new LogAnalyticsQuery
            {
                Text = query
            };

            return logAnalyticsQuery;
        }

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
