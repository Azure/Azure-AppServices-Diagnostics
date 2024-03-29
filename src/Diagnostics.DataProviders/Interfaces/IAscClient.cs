﻿using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IAscClient
    {
        /// <summary>
        /// Makes an HTTP POST request and retrieves the result in an object of type T.
        /// </summary>
        /// <param name="jsonPostBody">POST body in JSON format to submit to Azure Support Center ADS insight.</param>
        /// <param name="apiUri">API endpoint to connect to. It is /api/diagnosis/ for Insight Diagnostics and /api/diagnosticdata/ for Data Diagnostics</param>
        /// <param name="apiVersion">Api version to use which calling Azure Support Center.</param>
        /// <param name="cancellationToken">Cancellation Token for this operation.</param>
        /// <returns>Result of the HTTP Post Request.</returns>
        Task<T> MakeHttpPostRequest<T>(string jsonPostBody, string apiUri, string apiVersion, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Makes an HTTP GET request and retrieves the result in an object of type T.
        /// </summary>
        /// <param name="queryString">Querystring to include in the GET request to Azure Support Center.</param>
        /// <param name="apiUri">API endpoint to connect to. It is /api/diagnosis/ for Insight Diagnostics and /api/diagnosticdata/ for Data Diagnostics</param>
        /// <param name="apiVersion">Api version to use which calling Azure Support Center.</param>
        /// <param name="cancellationToken">Cancellation Token for this operation.</param>
        /// <returns>Result of the HTTP Get Request.</returns>
        Task<T> MakeHttpGetRequest<T>(string queryString, string apiUri, string apiVersion, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the Insight from the given Blob Url.
        /// </summary>
        /// <param name="blobUri">Blob uri including the saas token in the query string.</param>
        /// <param name="cancellationToken">Cancellation Token for this operation.</param>
        /// <returns>Contents from the blob uri.</returns>
        Task<T> GetInsightFromBlob<T>(string blobUri, CancellationToken cancellationToken = default(CancellationToken));
    }
}
