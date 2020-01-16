using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Data Provider to get Azure Support Center (ASC) ADS Insights.
    /// </summary>
    public class AscDataProvider : DiagnosticDataProvider, IAscDataProvider
    {
        private AscDataProviderConfiguration dataProviderConfiguration;

        private AscClient ascClient;

        private string dataProviderRequestId;

        private DataProviderContext CurrentRequestContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AscDataProvider"/> class.
        /// </summary>
        /// <param name="cache">Operation Data Cache instance.</param>
        /// <param name="configuration">Configuration for calling into Azure Support Center.</param>
        /// <param name="requestId">AppLens request id.</param>
        public AscDataProvider(OperationDataCache cache, AscDataProviderConfiguration configuration, string requestId, DataProviderContext context)
            : base(cache)
        {
            dataProviderConfiguration = configuration;
            dataProviderRequestId = requestId;
            ascClient = new AscClient(configuration, dataProviderRequestId, context.receivedHeaders);
            CurrentRequestContext = context;
        }

        /// <inheritdoc/>
        public Task<T> GetInsightFromBlob<T>(string blobUri, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ascClient.GetInsightFromBlob<T>(blobUri, cancellationToken);
        }

        public DataProviderMetadata GetMetadata()
        {
            return new DataProviderMetadata
            {
                ProviderName = "AzureSupportCenter"
            };
        }

        /// <inheritdoc/>
        public Task<T> MakeHttpGetRequest<T>(string queryString, string apiUri, string apiVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ascClient.MakeHttpGetRequest<T>(queryString, apiUri, apiVersion, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> MakeHttpGetRequest<T>(string queryString, string apiUri, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ascClient.MakeHttpGetRequest<T>(queryString, apiUri, string.Empty, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> MakeHttpGetRequest<T>(string queryString, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeHttpGetRequest<T>(queryString, string.Empty, string.Empty, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> MakeHttpPostRequest<T>(string jsonPostBody, string apiUri, string apiVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ascClient.MakeHttpPostRequest<T>(jsonPostBody, apiUri, apiVersion, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> MakeHttpPostRequest<T>(string jsonPostBody, string apiUri, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ascClient.MakeHttpPostRequest<T>(jsonPostBody, apiUri, string.Empty, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> MakeHttpPostRequest<T>(string jsonPostBody, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeHttpPostRequest<T>(jsonPostBody, string.Empty, string.Empty, cancellationToken);
        }
    }
}
