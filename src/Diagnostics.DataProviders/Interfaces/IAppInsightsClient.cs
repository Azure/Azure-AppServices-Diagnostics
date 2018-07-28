using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IAppInsightsClient
    {
        Task<DataTable> ExecuteQueryAsync(string query);
        void SetAppInsightsKey(string appId, string apiKey);
    }
}
