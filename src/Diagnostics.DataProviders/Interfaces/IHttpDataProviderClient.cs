using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Interfaces
{    
    public interface IHttpDataProviderClient
    {
        /// <summary>
        /// Make an HTTP GET request and get the response serialized in specified type.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="authOptions">Authentication scheme to be used while sending the request. Null value indicates anonymous request.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP GET Request serialized in specified type.</returns>
        Task<T> MakeHttpGetRequestAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders = null, HttpProviderAuthenticationOption authOptions = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make an HTTP GET request and get the response serialized in specified type.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestMessage">A object of type HttpRequestMessage that represents the HTTP GET request to be sent out.</param>
        /// <param name="authOptions">Authentication scheme to be used while sending the request. Null value indicates anonymous request.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP GET Request serialized in specified type.</returns>
        Task<T> MakeHttpGetRequestAsync<T>(HttpRequestMessage requestMessage, HttpProviderAuthenticationOption authOptions = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make an HTTP POST request and get the response serialized in specified type.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the POST request should be sent to.</param>
        /// <param name="jsonPostBody">Request body in JSON format.</param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="authOptions">Authentication scheme to be used while sending the request. Null value indicates anonymous request.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP POST Request serialized in specified type.</returns>
        Task<T> MakeHttpPostRequestAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders = null, HttpProviderAuthenticationOption authOptions = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make an HTTP POST request and get the response serialized in specified type.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestMessage">A object of type HttpRequestMessage that represents the HTTP POST request to be sent out.</param>
        /// <param name="authOptions">Authentication scheme to be used while sending the request. Null value indicates anonymous request.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP POST Request serialized in specified type.</returns>
        Task<T> MakeHttpPostRequestAsync<T>(HttpRequestMessage requestMessage, HttpProviderAuthenticationOption authOptions = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
