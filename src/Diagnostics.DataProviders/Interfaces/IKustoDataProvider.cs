using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IKustoDataProvider: IMetadataProvider
    {
        Task<DataTable> ExecuteQuery(string query, string requestId = null, string operationName = null);

        Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null);
    }
}
