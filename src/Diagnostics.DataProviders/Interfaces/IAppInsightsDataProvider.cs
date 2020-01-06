using System.Data;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IAppInsightsDataProvider : IMetadataProvider
    {
        Task<DataTable> ExecuteAppInsightsQuery(string query);

        Task<bool> SetAppInsightsKey(string appId, string apiKey);

        Task<bool> SetAppInsightsKey(OperationContext<IResource> cxt);
    }
}
