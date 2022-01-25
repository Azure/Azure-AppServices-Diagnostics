using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    internal class KustoLogDecorator : LogDecoratorBase, IKustoDataProvider
    {
        public IKustoDataProvider DataProvider;

        public KustoLogDecorator(DataProviderContext context, IKustoDataProvider dataProvider) : base((DiagnosticDataProvider)dataProvider, context, dataProvider.GetMetadata())
        {
            DataProvider = dataProvider;
        }

        public Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {
            return MakeDependencyCall(DataProvider.ExecuteQuery(query, stampName, _requestId, operationName));
        }

        public Task<DataTable> ExecuteQueryOnAllAppAppServiceClusters(string query, string operationName)
        {
            return MakeDependencyCall(DataProvider.ExecuteQueryOnAllAppAppServiceClusters(query, operationName));
        }

        public Task<DataTable> ExecuteQueryOnFewAppServiceClusters(List<string> appServiceClusterNames, string query, string operationName)
        {
            return MakeDependencyCall(DataProvider.ExecuteQueryOnFewAppServiceClusters(appServiceClusterNames, query, operationName));
        }

        public Task<DataTable> ExecuteClusterQuery(string query, string requestId = null, string operationName = null)
        {
            return MakeDependencyCall(DataProvider.ExecuteClusterQuery(query, _requestId, operationName));
        }

        public Task<DataTable> ExecuteClusterQuery(string query, string cluster, string databaseName, string requestId, string operationName)
        {
            return MakeDependencyCall(DataProvider.ExecuteClusterQuery(query, cluster, databaseName, _requestId, operationName));
        }

        public Task<KustoQuery> GetKustoQuery(string query, string clusterName, string databaseName = null, string operationName = null)
        {
            return MakeDependencyCall(DataProvider.GetKustoQuery(query, clusterName, databaseName, operationName));
        }

        public Task<KustoQuery> GetKustoQuery(string query, string stampName)
        {
            return MakeDependencyCall(DataProvider.GetKustoQuery(query, stampName));
        }

        public Task<KustoQuery> GetKustoClusterQuery(string query)
        {
            return MakeDependencyCall(DataProvider.GetKustoClusterQuery(query));
        }

        public Task<string> GetAggHighPerfClusterNameByStampAsync(string stampName)
        {
            return MakeDependencyCall(DataProvider.GetAggHighPerfClusterNameByStampAsync(stampName));
        }

        public Task<DataTable> ExecuteQueryOnHighPerfClusterWithFallback(string aggQuery, string backupQuery, string stampName, string requestId = null, string operationName = null)
        {
            return MakeDependencyCall(DataProvider.ExecuteQueryOnHighPerfClusterWithFallback(aggQuery, backupQuery, stampName, requestId, operationName));
        }
    }
}
