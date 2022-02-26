using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.DataProviders.KeyVaultCertLoader;
using Diagnostics.DataProviders.TokenService;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Authentication type to use while sending the HTTP request
    /// </summary>
    public enum HttpProviderAuthenticationSchemes
    {
        /// <summary>
        /// Anonymous authentication.
        /// </summary>
        None,

        /// <summary>
        /// Token acquired from AAD via certificate.
        /// </summary>
        AADToken,

        /// <summary>
        /// Client certificate authentication.
        /// </summary>
        ClientCert
    }

    public interface IHttpProviderAuthConfiguration
    {
        string CertificateSubjectName { get;}
    }

    /// <summary>
    /// Specifies client certificate auth configuration to be used.
    /// </summary>
    public class HttpProviderCertAuthConfiguration: IHttpProviderAuthConfiguration
    {
        /// <summary>
        /// Subject name of the certificate that is to be loaded.
        /// </summary>
        public string CertificateSubjectName { get; private set; }
        
        /// <summary>
        /// Specifies client certificate auth configuration to be used.
        /// </summary>
        /// <param name="certificateSubjectName">Subject name of the certificate that will be sent with the request when client cert auth is used.</param>
        public HttpProviderCertAuthConfiguration(string certificateSubjectName)
        {
            if (string.IsNullOrWhiteSpace(certificateSubjectName))
            {
                throw new ArgumentNullException(paramName:nameof(certificateSubjectName), message: "Unable to set auth options. Supplied certificate subject name is empty.");
            }
            try {
                certificateSubjectName = GenericCertLoader.Instance.GetCertBySubjectName(certificateSubjectName).Subject;
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentOutOfRangeException(paramName: nameof(certificateSubjectName), message: "Please contact the AppLens team to onboard this certificate. Once onboarded, the certificate can be used with HTTP provider.");
            }
            CertificateSubjectName = certificateSubjectName;
        }
    }

    /// <summary>
    /// Specifies AAD auth configuration to be used to acquire a token via trusted certificate.
    /// </summary>
    public class HttpProviderAADTokenAuthConfiguration : HttpProviderCertAuthConfiguration, IHttpProviderAuthConfiguration
    {
        /// <summary>
        /// Specifies AAD auth configuration to be used to acquire a token via trusted certificate.
        /// </summary>
        /// <param name="certificateSubjectName">Subject name of the certificate that is used to acquire aad token.</param>
        /// <param name="clientId">Client id of the AAD app that is to be contacted to issue the token.</param>
        /// <param name="aadAuthorityDomain">Domain URI of the tenant in which the AAD app resides.
        /// <br/>e.g.. https://login.microsoftonline.com/microsoft.onmicrosoft.com for the Microsoft tenant.</param>
        /// <param name="audienceWithScope">Target resource to whom the token will be sent to. Ensure the audience also includes required scope. The default scope is "{ResourceIdUri/.default}".
        /// <br/> e.g.. https://msazurecloud.onmicrosoft.com/azurediagnostic/.default </param>
        public HttpProviderAADTokenAuthConfiguration(string certificateSubjectName, string clientId, Uri aadAuthorityDomain, string audienceWithScope) : base(certificateSubjectName)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException(paramName: nameof(clientId), message: "Unable to set auth options. Supplied aad client id information is empty.");
            }
            
            if (aadAuthorityDomain == null)
            {
                throw new ArgumentNullException(paramName: nameof(aadAuthorityDomain), message: "Unable to set auth options. Supplied aad authority domain information is empty.");
            }

            if (audienceWithScope == null)
            {
                throw new ArgumentNullException(paramName: nameof(audienceWithScope), message: "Unable to set auth options. Please supply a resource id to whom the token will be sent to along with the scope of access required.");
            }

            ClientId = clientId;
            AadAuthorityDomain = aadAuthorityDomain;
            Audience = audienceWithScope;
        }

        /// <summary>
        /// Client id of the AAD app that is to be contacted to issue the token.
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// Domain URI of the tenant in which the AAD app resides.
        /// <br/>e.g.. https://login.microsoftonline.com/microsoft.onmicrosoft.com for the Microsoft tenant.
        /// </summary>
        public Uri AadAuthorityDomain { get; private set; }

        /// <summary>
        /// Target resource to whom the token will be sent to. Ensure the audience also includes required scope. The default scope is "{ResourceIdUri/.default}".
        /// <br/> e.g.. https://msazurecloud.onmicrosoft.com/azurediagnostic/.default 
        /// </summary>
        public string Audience { get; private set; }
    }

    /// <summary>
    /// Specifies the authentication scheme and supporting options to use while making an outbound HTTP call.
    /// </summary>
    public class HttpProviderAuthenticationOption
    {
        /// <summary>
        /// Authentication scheme to use.
        /// </summary>
        public HttpProviderAuthenticationSchemes AuthenticationScheme { get; private set; }

        /// <summary>
        /// Configuration supporting the desired authentication scheme.
        /// <br/>An object of type HttpProviderAADTokenAuthConfiguration for HttpProviderAuthenticationSchemes.AADToken
        /// <br/>An object of type HttpProviderCertAuthConfiguration for HttpProviderAuthenticationSchemes.ClientCert
        /// <br/>A null value is permitted only when the authenticationScheme is set to HttpProviderAuthenticationSchemes.None
        /// </summary>
        public IHttpProviderAuthConfiguration HttpProviderAuthConfiguration { get; private set; }

        /// <summary>
        /// Certificate subject name that is to be loaded.
        /// </summary>
        public string CertificateSubjectName { get; private set; } = string.Empty;

        /// <summary>
        /// Client id of the AAD app hat is to be contacted to acquire a token.
        /// <br/> Valid only if AAD authententication was specified.
        /// </summary>
        public string ClientId { get; private set; } = string.Empty;

        /// <summary>
        /// Domain URI of the tenant in which the AAD app resides.
        /// <br/>e.g.. https://login.microsoftonline.com/microsoft.onmicrosoft.com for the Microsoft tenant.
        /// <br/> Valid only if AAD authententication was specified.
        /// </summary>
        public Uri AadAuthorityDomain { get; private set; } = null;

        /// <summary>
        /// Target resource to whom the token will be sent to. Ensure the audience also includes required scope. The default scope is "{ResourceIdUri/.default}".
        /// <br/> e.g.. https://msazurecloud.onmicrosoft.com/azurediagnostic/.default 
        /// <br/> Valid only if AAD authententication was specified.
        /// </summary>
        public string Audience { get; private set; } = string.Empty;

        private void InitValues(HttpProviderAuthenticationSchemes authenticationScheme, string certificateSubjectName, string clientId, Uri aadAuthorityDomain, string audienceWithScope)
        {
            AuthenticationScheme = authenticationScheme;
            CertificateSubjectName = certificateSubjectName;
            ClientId = clientId;
            AadAuthorityDomain = aadAuthorityDomain;
            Audience = audienceWithScope;
        }

        /// <summary>
        /// Specifies the authentication scheme and supporting options to use while making an outbound HTTP call.
        /// </summary>
        /// <param name="authenticationScheme">Desired authentication to use while sending outbound HTTP request. None indicated Anonymous request.</param>
        /// <param name="certificateSubjectName">Certificate that is to be loaded</param>
        /// <param name="clientId">Client id of the AAD app hat is to be contacted to acquire a token.
        /// <br/> Valid only if AAD authententication was specified.</param>
        /// <param name="aadAuthorityDomain">Domain URI of the tenant in which the AAD app resides.
        /// <br/>e.g.. https://login.microsoftonline.com/microsoft.onmicrosoft.com for the Microsoft tenant.
        /// <br/> Valid only if AAD authententication was specified.</param>
        /// <param name="audienceWithScope">Target resource to whom the token will be sent to. Ensure the audience also includes required scope. The default scope is "{ResourceIdUri/.default}".
        /// <br/> e.g.. https://msazurecloud.onmicrosoft.com/azurediagnostic/.default 
        /// <br/> Valid only if AAD authententication was specified.</param>
        public HttpProviderAuthenticationOption(HttpProviderAuthenticationSchemes authenticationScheme, string certificateSubjectName,
            string clientId, Uri aadAuthorityDomain, string audienceWithScope)
        {
            if (authenticationScheme == HttpProviderAuthenticationSchemes.ClientCert)
            {
                HttpProviderAuthConfiguration = new HttpProviderCertAuthConfiguration(certificateSubjectName);
                InitValues(authenticationScheme, certificateSubjectName, string.Empty, null, string.Empty);
            }

            if (authenticationScheme == HttpProviderAuthenticationSchemes.AADToken)
            {
                HttpProviderAuthConfiguration = new HttpProviderAADTokenAuthConfiguration(certificateSubjectName, clientId, aadAuthorityDomain, audienceWithScope);
                InitValues(authenticationScheme, certificateSubjectName, clientId, aadAuthorityDomain, audienceWithScope);
            }
        }

        /// <summary>
        /// Specifies the authentication scheme and supporting options to use while making an outbound HTTP call.
        /// </summary>
        /// <param name="authenticationScheme">Desired authentication to use while sending outbound HTTP request. None indicated Anonymous request.</param>
        /// <param name="httpProviderAuthConfiguration">Configuration to support the desired authentication scheme. 
        /// <br/>An object of type HttpProviderAADTokenAuthConfiguration for HttpProviderAuthenticationSchemes.AADToken
        /// <br/>An object of type HttpProviderCertAuthConfiguration for HttpProviderAuthenticationSchemes.ClientCert
        /// <br/>A null value is permitted only when the authenticationScheme is set to HttpProviderAuthenticationSchemes.None</param>
        public HttpProviderAuthenticationOption(HttpProviderAuthenticationSchemes authenticationScheme, IHttpProviderAuthConfiguration httpProviderAuthConfiguration = null)
        {
            switch (authenticationScheme)
            {
                case HttpProviderAuthenticationSchemes.AADToken:
                    if (httpProviderAuthConfiguration is HttpProviderAADTokenAuthConfiguration)
                    {
                        HttpProviderAADTokenAuthConfiguration aadTokenAuthConfiguration = httpProviderAuthConfiguration as HttpProviderAADTokenAuthConfiguration;
                        InitValues(authenticationScheme, aadTokenAuthConfiguration.CertificateSubjectName, aadTokenAuthConfiguration.ClientId, aadTokenAuthConfiguration.AadAuthorityDomain, aadTokenAuthConfiguration.Audience);
                        HttpProviderAuthConfiguration = aadTokenAuthConfiguration;

                    }
                    else
                    {
                        throw new NotSupportedException(message: $"Only objects of type HttpProviderAADTokenAuthConfiguration are supported with AADToken authentication scheme.");
                    }
                    break;
                case HttpProviderAuthenticationSchemes.ClientCert:
                    if (httpProviderAuthConfiguration is HttpProviderCertAuthConfiguration)
                    {
                        HttpProviderCertAuthConfiguration certAuthConfiguration = httpProviderAuthConfiguration as HttpProviderCertAuthConfiguration;
                        InitValues(authenticationScheme, certAuthConfiguration.CertificateSubjectName, string.Empty, null, string.Empty);
                        HttpProviderAuthConfiguration = certAuthConfiguration;
                    }
                    else
                    {
                        throw new NotSupportedException(message: $"Only objects of type HttpProviderCertAuthConfiguration are supported with ClientCert authentication scheme.");
                    }
                    break;
                default:
                    HttpProviderAuthConfiguration = null;
                    break;
            }
        }
    }

    /// <summary>
    /// Client to facilitate making outbound HTTP GET and POST requests.
    /// <br/>Supported authentication schemes are Anonymous, AAD token acquired via trusted certificate and Client certificate.
    /// <br/>Certificate in question should be pre-loaded in memory from KeyVault.
    /// </summary>
    public class HttpDataProviderClient:IHttpDataProviderClient
    {
        private const string DEFAULT_CACHE_KEY_NAME = "ALL";

        private string GetHttpClientCacheKey(string certificateSubjectName)
        {
            if (!string.IsNullOrWhiteSpace(certificateSubjectName))
            {
                return !certificateSubjectName.StartsWith("CN=", StringComparison.CurrentCultureIgnoreCase)
                    ? $"CN={certificateSubjectName}".ToUpperInvariant()
                    : certificateSubjectName.ToUpperInvariant();
            }
            else
            {
                return DEFAULT_CACHE_KEY_NAME;
            }
        }

        /// <summary>
        /// Holds the http clients used to make outbound requests as identified by the key.
        /// As per the current logic, unique httpCient for every client certificate intended for outbound HTTP requests and one more for rest of the reqests (AAD + Anonymous)
        /// </summary>
        private static readonly Lazy<ConcurrentDictionary<string, HttpClient>> clientCollection = new Lazy<ConcurrentDictionary<string, HttpClient>>(() =>
        {
            ConcurrentDictionary<string, HttpClient> clientCollection = new ConcurrentDictionary<string, HttpClient>();
            return clientCollection;
        });

        private HttpDataProviderConfiguration _config = null;

        private DataProviderContext _dpContext = null;

        private HttpClient GetHttpClientFromCache(string certificateSubjectName)
        {
            if (!HttpDataProviderClient.clientCollection.Value.ContainsKey(GetHttpClientCacheKey(certificateSubjectName)))
            {
                _ = GetHttpClientCacheKey(certificateSubjectName) == DEFAULT_CACHE_KEY_NAME
                    ? HttpDataProviderClient.clientCollection.Value.TryAdd(DEFAULT_CACHE_KEY_NAME, CreateHttpClient(ignoreServerCertificateErrors: false))
                    : HttpDataProviderClient.clientCollection.Value.TryAdd(GetHttpClientCacheKey(certificateSubjectName), CreateHttpClient(certificateSubjectName, ignoreServerCertificateErrors: false));
            }
            HttpDataProviderClient.clientCollection.Value.TryGetValue(GetHttpClientCacheKey(certificateSubjectName), out HttpClient httpClient);
            return httpClient;
        }

        /// <summary>
        /// Creates a new HttpDataProviderClient with the default authentication mode set to Anonymous.
        /// </summary>
        /// <param name="config">An object of type HttpDataProviderConfiguration representing default values</param>
        /// <param name="dpContext">Context for the data provider.</param>
        public HttpDataProviderClient( HttpDataProviderConfiguration config, DataProviderContext dpContext)
        {
            if (config == null)
            {
                throw new ArgumentNullException(paramName: nameof(config), message: "Unable to create HttpDataProvider. Please pass valid non-empty data provider configuration.");
            }

            if (dpContext == null)
            {
                throw new ArgumentNullException(paramName: nameof(dpContext), message: "Unable to create HttpDataProvider. Please pass valid non-empty data provider context.");
            }
            _config = config;
            _dpContext = dpContext;
        }
        

        private HttpClient CreateHttpClient(string certificateSubjectName = "", bool ignoreServerCertificateErrors = false)
        {
            var handler = new HttpClientHandler();
            handler.MaxConnectionsPerServer = _config.MaxConnectionsPerServer;
            handler.MaxAutomaticRedirections = 5;

            if (ignoreServerCertificateErrors)
            {
                handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                };
            }

            if (!string.IsNullOrWhiteSpace(certificateSubjectName) && !certificateSubjectName.Equals(GetHttpClientCacheKey(string.Empty),StringComparison.OrdinalIgnoreCase) 
                && GenericCertLoader.Instance.GetCertBySubjectName(certificateSubjectName) != null)
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ClientCertificates.Add(GenericCertLoader.Instance.GetCertBySubjectName(certificateSubjectName));
            }

            var httpClient = new HttpClient(handler, true);

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_config.UserAgent);            
            httpClient.Timeout = TimeSpan.FromMilliseconds(_config.DefaultRequestTimeOutInMilliSeconds);

            return httpClient;
        }

        private void ValidateAndAddRequestMessageHeaders(HttpRequestMessage requestMessage, Dictionary<string, string> additionalHeaders)
        {   
            Dictionary<string, IEnumerable<string>> originalHeaderCollection = new Dictionary<string, IEnumerable<string>>();
            if (requestMessage != null)
            {
                foreach (var header in requestMessage.Headers)
                {
                    if (!_config.ProhibitedHeadersList.Contains(header.Key.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        originalHeaderCollection.TryAdd(header.Key, header.Value);
                    }
                }

                requestMessage.Headers.Clear();

                //Add request id for every outbound request.
                requestMessage.Headers.TryAddWithoutValidation(Diagnostics.Logger.HeaderConstants.RequestIdHeaderName, _dpContext.RequestId);

                foreach (var header in originalHeaderCollection)
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (additionalHeaders != null)
                {
                    foreach (var header in additionalHeaders)
                    {
                        if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value) 
                            && !_config.ProhibitedHeadersList.Contains(header.Key.Trim(), StringComparer.OrdinalIgnoreCase))
                        {
                            requestMessage.Headers.Add(header.Key, header.Value);
                        }
                    }
                }
                
            }
        }

        /// <inheritdoc/>
        public async Task<T> MakeHttpGetRequestAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders = null, HttpProviderAuthenticationOption authOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureUriValid(requestUri);

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                ValidateAndAddRequestMessageHeaders(requestMessage, additionalHeaders);
                return await MakeHttpGetRequestAsync<T>(requestMessage, authOptions, cancellationToken).ConfigureAwait(true);
            }   
        }

        /// <inheritdoc/>
        public async Task<T> MakeHttpGetRequestAsync<T>(HttpRequestMessage requestMessage, HttpProviderAuthenticationOption authOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureRequestTypeAndUriValid(requestMessage, HttpMethod.Get);
            return await SendRequestAsync<T>(requestMessage, authOptions, cancellationToken).ConfigureAwait(true);
        }

        /// <inheritdoc/>
        public async Task<T> MakeHttpPostRequestAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders = null, HttpProviderAuthenticationOption authOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureUriValid(requestUri);

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri))
            {
                ValidateAndAddRequestMessageHeaders(requestMessage, additionalHeaders);                
                requestMessage.Content = new StringContent(jsonPostBody, Encoding.UTF8, "application/json");

                return await MakeHttpPostRequestAsync<T>(requestMessage, authOptions, cancellationToken).ConfigureAwait(true);
            }
        }

        /// <inheritdoc/>
        public async Task<T> MakeHttpPostRequestAsync<T>(HttpRequestMessage requestMessage, HttpProviderAuthenticationOption authOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureRequestTypeAndUriValid(requestMessage, HttpMethod.Post);
            return await SendRequestAsync<T>(requestMessage, authOptions, cancellationToken).ConfigureAwait(true);
        }

        private async Task<T> SendRequestAsync<T>(HttpRequestMessage requestMessage, HttpProviderAuthenticationOption authOptions, CancellationToken cancellationToken = default(CancellationToken))
        {
            requestMessage.Headers.Remove(HeaderNames.Authorization);

            if (authOptions?.AuthenticationScheme == HttpProviderAuthenticationSchemes.AADToken)
            {
                requestMessage.Headers.Add(HeaderNames.Authorization,
                    await TokenRequestorFromPFXService.Instance.GetAuthorizationTokenAsync(authOptions.ClientId, authOptions.AadAuthorityDomain, authOptions.Audience, authOptions.CertificateSubjectName, true)
                    .ConfigureAwait(true));
            }

            ValidateAndAddRequestMessageHeaders(requestMessage, null);

            HttpResponseMessage response = await GetHttpClientFromCache(GetHttpClientCacheKey(authOptions?.CertificateSubjectName)).SendAsync(requestMessage, cancellationToken).ConfigureAwait(true);
            string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            try
            {
                response.EnsureSuccessStatusCode();
                if (typeof(T).Equals(typeof(string)))
                {
                    return CastTo<T>(responseContent);
                }
                else
                {
                    T value;
                    try
                    {
                        value = JsonConvert.DeserializeObject<T>(responseContent);
                    }
                    catch (JsonSerializationException serializeException)
                    {
                        serializeException.Data.Add("Raw HTTP Response", responseContent);
                        throw new JsonSerializationException($"Failed to serialize HTTP response to type {typeof(T)}", serializeException);
                    }

                    return value;
                }
            }
            catch (HttpRequestException ex)
            {
                ex.Data.Add("StatusCode", response.StatusCode);
                ex.Data.Add("ResponseContent", responseContent);
                throw;
            }
            finally
            {
                response.Dispose();
            }
        }

        private void EnsureUriValid(Uri requestUri)
        {
            if (requestUri == null)
                throw new ArgumentNullException(paramName: nameof(requestUri), message: "Supplied URI is null. Please specify an absolute URI where the HTTP Request should be sent out to.");
            if (!requestUri.IsAbsoluteUri)
                throw new ArgumentOutOfRangeException(paramName: nameof(requestUri), message: "Supplied URI is not absolute. Please specify an absolute URI where the HTTP Request should be sent out to.");
        }

        private void EnsureRequestTypeAndUriValid(HttpRequestMessage requestMessage, HttpMethod validType)
        {
            if (requestMessage == null)
                throw new ArgumentException(paramName: nameof(requestMessage), message: "Supplied request message is null.");

            if (requestMessage.Method != validType)
                throw new ArgumentOutOfRangeException(paramName: nameof(requestMessage), message: $"The supplied HttpRequestMessage object is not of type {validType}.");

            EnsureUriValid(requestMessage.RequestUri);
        }

        private static T CastTo<T>(object obj)
        {
            try
            {
                return (T)obj;
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Failed to cast object from {obj.GetType()} to {typeof(T)}", ex);
            }
        }
    }
}
