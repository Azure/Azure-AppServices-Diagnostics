using Diagnostics.ModelsAndUtils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IKustoClient
    {
        Task<DataTableResponseObject> ExecuteQueryAsync(string query, string stampName, string requestId = null, string operationName = null);
    }
}
