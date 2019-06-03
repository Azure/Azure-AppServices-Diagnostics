using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IKustoClient
    {
        Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, int timeoutSeconds, string requestId = null, string operationName = null);

        Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, string requestId = null, string operationName = null);

        Task<KustoQuery> GetKustoQueryAsync(string query, string cluster, string database);
    }
}
