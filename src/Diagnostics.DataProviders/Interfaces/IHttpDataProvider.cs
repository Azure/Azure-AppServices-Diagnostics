using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IHttpDataProvider : IMetadataProvider
    {
        #region GET requests
        /// <summary>
        /// Make HTTP GET request with Anonymous authentication. No credentials required.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>        
        /// <returns>Result of the HTTP GET request casted into specified type.</returns>
        Task<T> SendGetRequestAnonyousAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders  = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make HTTP GET request, sending the default AppLens certificate as client certificate along with the request.
        /// <br/> Use this if the destination supports client certificate based authentication.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP GET request casted into specified type.</returns>
        Task<T> SendGetRequestCertAuthDefaultAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make HTTP GET request, sending the certificate specified by the certificate subject name as a client certificate along with the request.
        /// <br/> Use this if the destination supports client certificate based authentication.
        /// <br/> Note: The certificate in question needs to be onboared. Attaching raw certificate is not supported.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="certificateSubjectName">Subject name of the certificate that will be attached to the request.</param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP GET request casted into specified type.</returns>
        Task<T> SendGetRequestCertAuthCustomAsync<T>(Uri requestUri, string certificateSubjectName, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make HTTP GET request, sending a token acquired from AppLensToExternal_AME aad app in the AME tenant.
        /// <br/> Use this if the destination supports aad token based authentication.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="audienceWithScope">Target resource to whom the token will be sent to. Ensure the audience also includes required scope. The default scope is "{ResourceIdUri/.default}".
        /// <br/>e.g.. https://msazurecloud.onmicrosoft.com/azurediagnostic/.default </param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP GET request casted into specified type.</returns>
        Task<T> SendGetRequestAadAuthDefaultAsync<T>(Uri requestUri, string audienceWithScope, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make HTTP GET request, sending a token acquired from the specified aad app.
        /// <br/> Use this if the destination supports aad token based authentication.
        /// <br/> Note: The certificate used to acquire aad token needs to be onboared. Raw certificates are not supported.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="certificateSubjectName">Subject name of the certificate that is used to acquire aad token.</param>
        /// <param name="aadAppClientId">Client id of the AAD app that is to be contacted to issue the token.</param>
        /// <param name="aadAuthorityDomainUri">Domain URI of the tenant in which the AAD app resides.
        /// <br/>e.g.. https://login.microsoftonline.com/microsoft.onmicrosoft.com for the Microsoft tenant.</param>
        /// <param name="audienceWithScope">Target resource to whom the token will be sent to. Ensure the audience also includes required scope. The default scope is "{ResourceIdUri/.default}".
        /// <br/>e.g.. https://msazurecloud.onmicrosoft.com/azurediagnostic/.default </param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP GET request casted into specified type.</returns>
        Task<T> SendGetRequestAadAuthCustomAsync<T>(Uri requestUri, string certificateSubjectName, string aadAppClientId, Uri aadAuthorityDomainUri, string audienceWithScope, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make HTTP GET request.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="authOptions">Authentication scheme to be used while sending the request. Null value indicates anonymous request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP GET request casted into specified type.</returns>
        Task<T> SendGetRequestAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders = null, HttpProviderAuthenticationOption authOptions = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));
        #endregion

        #region POST requests
        /// <summary>
        /// Make HTTP POST request with Anonymous authentication. No credentials required.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="jsonPostBody">POST body in JSON format to send along with the request.</param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP POST request casted into specified type.</returns>
        Task<T> SendPostRequestAnonyousAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make HTTP POST request, sending the default AppLens certificate as client certificate along with the request.
        /// <br/> Use this if the destination supports client certificate based authentication.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="jsonPostBody">POST body in JSON format to send along with the request.</param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP POST request casted into specified type.</returns>
        Task<T> SendPostRequestCertAuthDefaultAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make HTTP POST request, sending the certificate specified by the certificate subject name as a client certificate along with the request.
        /// <br/> Use this if the destination supports client certificate based authentication.
        /// <br/> Note: The certificate in question needs to be onboared. Attaching raw certificate is not supported.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="jsonPostBody">POST body in JSON format to send along with the request.</param>
        /// <param name="certificateSubjectName">Subject name of the certificate that will be attached to the request.</param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP POST request casted into specified type.</returns>
        Task<T> SendPostRequestCertAuthCustomAsync<T>(Uri requestUri, string jsonPostBody, string certificateSubjectName, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make HTTP POST request, sending a token acquired from AppLensToExternal_AME aad app in the AME tenant.
        /// <br/> Use this if the destination supports aad token based authentication.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="jsonPostBody">POST body in JSON format to send along with the request.</param>
        /// <param name="audienceWithScope">Target resource to whom the token will be sent to. Ensure the audience also includes required scope. The default scope is "{ResourceIdUri/.default}".
        /// <br/>e.g.. https://msazurecloud.onmicrosoft.com/azurediagnostic/.default </param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP POST request casted into specified type.</returns>
        Task<T> SendPostRequestAadAuthDefaultAsync<T>(Uri requestUri, string jsonPostBody, string audienceWithScope, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make HTTP POST request, sending a token acquired from the specified aad app.
        /// <br/> Use this if the destination supports aad token based authentication.
        /// <br/> Note: The certificate used to acquire aad token needs to be onboared. Raw certificates are not supported.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="jsonPostBody">POST body in JSON format to send along with the request.</param>
        /// <param name="certificateSubjectName">Subject name of the certificate that is used to acquire aad token.</param>
        /// <param name="aadAppClientId">Client id of the AAD app that is to be contacted to issue the token.</param>
        /// <param name="aadAuthorityDomainUri">Domain URI of the tenant in which the AAD app resides.
        /// <br/>e.g.. https://login.microsoftonline.com/microsoft.onmicrosoft.com for the Microsoft tenant.</param>
        /// <param name="audienceWithScope">Target resource to whom the token will be sent to. Ensure the audience also includes required scope. The default scope is "{ResourceIdUri/.default}".
        /// <br/>e.g.. https://msazurecloud.onmicrosoft.com/azurediagnostic/.default </param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP POST request casted into specified type.</returns>
        Task<T> SendPostRequestAadAuthCustomAsync<T>(Uri requestUri, string jsonPostBody, string certificateSubjectName, string aadAppClientId, Uri aadAuthorityDomainUri, string audienceWithScope, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Make HTTP POST request.
        /// </summary>
        /// <typeparam name="T">Type in which the response should be serialized to.</typeparam>
        /// <param name="requestUri">Absolute uri where the GET request should be sent to.</param>
        /// <param name="jsonPostBody">POST body in JSON format to send along with the request.</param>
        /// <param name="additionalHeaders">Additional headers to be sent along with the request.</param>
        /// <param name="authOptions">Authentication scheme to be used while sending the request. Null value indicates anonymous request.</param>
        /// <param name="operationName">Name identifying this call.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>Result of the HTTP POST request casted into specified type.</returns>
        Task<T> SendPostRequestAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders = null, HttpProviderAuthenticationOption authOptions = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken));
        #endregion
    }
}
