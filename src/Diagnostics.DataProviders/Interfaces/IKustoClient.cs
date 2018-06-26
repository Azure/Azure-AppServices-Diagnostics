using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IKustoClient
    {
        Task<DataTable> ExecuteQueryAsync(string query, string requestId = null, string operationName = null);

        Task<DataTable> ExecuteQueryAsync(string query, string stampName, string requestId = null, string operationName = null);

        Task<string> GetKustoQueryUriAsync(string stampName, string query);
    }
}
