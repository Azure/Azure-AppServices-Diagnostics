using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IAppInsightsDataProvider : IMetadataProvider
    {
        Task<DataTable> ExecuteAppInsightsQuery(string query);
        Task<bool> SetAppInsightsKey(string appId, string apiKey);
    }
}
