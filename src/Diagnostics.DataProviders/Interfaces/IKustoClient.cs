using System;
using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IKustoClient
    {
        Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, int timeoutSeconds, string requestId = null, string operationName = null, DateTime? startTime = null, DateTime? endTime = null);

        Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, string requestId = null, string operationName = null, DateTime? startTime = null, DateTime? endTime = null);

        Task<KustoQuery> GetKustoQueryAsync(string query, string cluster, string database);

        Task<KustoQuery> GetKustoQueryAsync(string query, string cluster, string database, string operationName = null);
    }
}
