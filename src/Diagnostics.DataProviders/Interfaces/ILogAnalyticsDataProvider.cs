using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface ILogAnalyticsDataProvider : IMetadataProvider
    {
        Task<DataTable> ExecuteQueryAsync(string query);
    }
}
