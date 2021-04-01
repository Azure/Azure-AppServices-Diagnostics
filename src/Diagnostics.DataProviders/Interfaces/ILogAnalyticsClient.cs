using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Interfaces
{
    interface ILogAnalyticsClient
    {
        public Task<DataTable> ExecuteQueryAsync(string query);
    }
}
