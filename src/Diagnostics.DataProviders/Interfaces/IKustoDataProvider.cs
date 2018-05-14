using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IKustoDataProvider
    {
        Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null);
    }
}
