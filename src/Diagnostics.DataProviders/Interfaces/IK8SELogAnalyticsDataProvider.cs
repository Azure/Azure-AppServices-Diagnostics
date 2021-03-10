using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IK8SELogAnalyticsDataProvider : IMetadataProvider
    {
        Task<DataTable> ExecuteQueryAsync(string query);
    }
}
