using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IKustoDataProvider : IMetadataProvider
    {
        Task<DataTable> ExecuteClusterQuery(string query, string requestId = null, string operationName = null);

        Task<DataTable> ExecuteClusterQuery(string query, string cluster, string databaseName, string requestId, string operationName);

        Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null);

        Task<DataTable> ExecuteQueryOnAllAppAppServiceClusters(string query, string operationName);

        Task<DataTable> ExecuteQueryOnFewAppServiceClusters(List<string> appServiceClusterNames, string query, string operationName);

        Task<KustoQuery> GetKustoQuery(string query, string stampName);

        Task<KustoQuery> GetKustoQuery(string query, string clusterName, string databaseName = null, string operationName = null);

        Task<KustoQuery> GetKustoClusterQuery(string query);

        Task<string> GetAggHighPerfClusterNameByStampAsync(string stampName);

        Task<DataTable> ExecuteQueryOnHighPerfClusterWithFallback(string aggQuery, string backupQuery, string stampName, string requestId = null, string operationName = null);

    }
}
