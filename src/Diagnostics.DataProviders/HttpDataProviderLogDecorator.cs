using Diagnostics.DataProviders.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class HttpDataProviderLogDecorator : LogDecoratorBase, IHttpDataProvider
    {
        private IHttpDataProvider _dataProvider;

        public HttpDataProviderLogDecorator(DataProviderContext context, IHttpDataProvider dataProvider) : base(context, dataProvider.GetMetadata())
        {
            _dataProvider = dataProvider;
        }

        Task<T> IHttpDataProvider.SendGetRequestAadAuthCustomAsync<T>(Uri requestUri, string certificateSubjectName, string aadAppClientId, Uri aadAuthorityDomainUri, string audienceWithScope, Dictionary<string, string> additionalHeaders, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendGetRequestAadAuthCustomAsync<T>(requestUri, certificateSubjectName, aadAppClientId, aadAuthorityDomainUri, audienceWithScope, additionalHeaders, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendGetRequestAadAuthDefaultAsync<T>(Uri requestUri, string audienceWithScope, Dictionary<string, string> additionalHeaders, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendGetRequestAadAuthDefaultAsync<T>(requestUri, audienceWithScope, additionalHeaders, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendGetRequestAnonymousAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendGetRequestAnonymousAsync<T>(requestUri, additionalHeaders, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendGetRequestAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders, HttpProviderAuthenticationOption authOptions, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendGetRequestAsync<T>(requestUri, additionalHeaders, authOptions, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendGetRequestCertAuthCustomAsync<T>(Uri requestUri, string certificateSubjectName, Dictionary<string, string> additionalHeaders, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendGetRequestCertAuthCustomAsync<T>(requestUri, certificateSubjectName, additionalHeaders, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendGetRequestCertAuthDefaultAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendGetRequestCertAuthDefaultAsync<T>(requestUri, additionalHeaders, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendPostRequestAadAuthCustomAsync<T>(Uri requestUri, string jsonPostBody, string certificateSubjectName, string aadAppClientId, Uri aadAuthorityDomainUri, string audienceWithScope, Dictionary<string, string> additionalHeaders, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendPostRequestAadAuthCustomAsync<T>(requestUri, jsonPostBody, certificateSubjectName, aadAppClientId, aadAuthorityDomainUri, audienceWithScope, additionalHeaders, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendPostRequestAadAuthDefaultAsync<T>(Uri requestUri, string jsonPostBody, string audienceWithScope, Dictionary<string, string> additionalHeaders, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendPostRequestAadAuthDefaultAsync<T>(requestUri, jsonPostBody, audienceWithScope, additionalHeaders, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendPostRequestAnonymousAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendPostRequestAnonymousAsync<T>(requestUri, jsonPostBody, additionalHeaders, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendPostRequestAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders, HttpProviderAuthenticationOption authOptions, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendPostRequestAsync<T>(requestUri, jsonPostBody, additionalHeaders, authOptions, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendPostRequestCertAuthCustomAsync<T>(Uri requestUri, string jsonPostBody, string certificateSubjectName, Dictionary<string, string> additionalHeaders, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendPostRequestCertAuthCustomAsync<T>(requestUri, jsonPostBody, certificateSubjectName, additionalHeaders, operationName, cancellationToken));
        }

        Task<T> IHttpDataProvider.SendPostRequestCertAuthDefaultAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders, string operationName, CancellationToken cancellationToken)
        {
            return MakeDependencyCall(_dataProvider.SendPostRequestCertAuthDefaultAsync<T>(requestUri, jsonPostBody, additionalHeaders, operationName, cancellationToken));
        }
    }
}
