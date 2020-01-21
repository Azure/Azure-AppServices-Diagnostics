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

		public Task<DataTable> ExecuteClusterQuery(string query, string requestId = null, string operationName = null)
		{
			return ExecuteQuery(query, DataProviderConstants.FakeStampForAnalyticsCluster, requestId, operationName);
		}

		public Task<KustoQuery> GetKustoQuery(string query, string stampName)
		{
			return MakeDependencyCall(DataProvider.GetKustoQuery(query, stampName));
		}

		public Task<KustoQuery> GetKustoClusterQuery(string query)
		{
			return MakeDependencyCall(DataProvider.GetKustoClusterQuery(query));
		}
	}
}
