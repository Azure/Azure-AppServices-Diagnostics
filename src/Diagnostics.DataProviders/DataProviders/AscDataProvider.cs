using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;
using System.Linq;
using System.Threading;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="AscDataProvider"/> class.
        /// </summary>
        /// <param name="cache">Operation Data Cache instance.</param>
        /// <param name="configuration">Configuration for calling into Azure Support Center.</param>
        /// <param name="requestId">AppLens request id.</param>
        public AscDataProvider(OperationDataCache cache, AscDataProviderConfiguration configuration, string requestId)
            : base(cache)
        {
            dataProviderConfiguration = configuration;
            dataProviderRequestId = requestId;
            ascClient = new AscClient(configuration, dataProviderRequestId);
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
        public Task<T> MakeHttpGetRequest<T>(string queryString, string apiVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ascClient.MakeHttpGetRequest<T>(queryString, apiVersion, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> MakeHttpGetRequest<T>(string queryString, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeHttpGetRequest<T>(queryString, string.Empty, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> MakeHttpPostRequest<T>(string jsonPostBody, string apiVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeHttpPostRequest<T>(jsonPostBody, apiVersion, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> MakeHttpPostRequest<T>(string jsonPostBody, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeHttpPostRequest<T>(jsonPostBody, string.Empty, cancellationToken);
        }
    }
}
