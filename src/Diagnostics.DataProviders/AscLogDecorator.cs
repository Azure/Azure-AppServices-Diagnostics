using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;

namespace Diagnostics.DataProviders
{
    internal class AscLogDecorator : LogDecoratorBase, IAscDataProvider
    {
        public IAscDataProvider DataProvider;

        public AscLogDecorator(DataProviderContext context, IAscDataProvider dataProvider) : base(context, dataProvider.GetMetadata())
        {
            DataProvider = dataProvider;
        }

        Task<T> IAscDataProvider.MakeHttpPostRequest<T>(string jsonPostBody, string apiUri, string apiVersion, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(DataProvider.MakeHttpPostRequest<T>(jsonPostBody, apiUri, apiVersion, cancellationToken));
        }

        Task<T> IAscDataProvider.MakeHttpPostRequest<T>(string jsonPostBody, string apiUri, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(DataProvider.MakeHttpPostRequest<T>(jsonPostBody, apiUri, cancellationToken));
        }

        Task<T> IAscDataProvider.MakeHttpPostRequest<T>(string jsonPostBody, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(DataProvider.MakeHttpPostRequest<T>(jsonPostBody, cancellationToken));
        }

        Task<T> IAscDataProvider.MakeHttpGetRequest<T>(string queryString, string apiUri, string apiVersion, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(DataProvider.MakeHttpGetRequest<T>(queryString, apiUri, apiVersion, cancellationToken));
        }

        Task<T> IAscDataProvider.MakeHttpGetRequest<T>(string queryString, string apiUri, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(DataProvider.MakeHttpGetRequest<T>(queryString, apiUri, cancellationToken));
        }

        Task<T> IAscDataProvider.MakeHttpGetRequest<T>(string queryString, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(DataProvider.MakeHttpGetRequest<T>(queryString, cancellationToken));
        }

        Task<T> IAscDataProvider.GetInsightFromBlob<T>(string blobUri, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(DataProvider.GetInsightFromBlob<T>(blobUri, cancellationToken));
        }
    }
}
