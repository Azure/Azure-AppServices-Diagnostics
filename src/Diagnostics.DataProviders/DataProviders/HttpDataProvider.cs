using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Metadata to show in Data Sources tab.
    /// </summary>
    public class HttpCallInfo
    {
        public string Text = string.Empty;
        public string Url = string.Empty;
        public string OperationName = string.Empty;
    }

    /// <summary>
    /// Data provider to make outbound HTTP calls. Supports three types of authentication for outbound calls
    /// <br/>Anonymous, AAD Token, Client Certificate
    /// </summary>
    public class HttpDataProvider : DiagnosticDataProvider, IHttpDataProvider
    {
        private HttpDataProviderConfiguration _config;
        private string _requestId;
        private HttpDataProviderClient _httpDataProviderClient;
        private DataProviderContext _requestContext;
        public HttpDataProvider(OperationDataCache cache, HttpDataProviderConfiguration configuration, DataProviderContext context)
           : base(cache, configuration)
        {
            _config= configuration;
            _requestId = context?.RequestId;
            _httpDataProviderClient = new HttpDataProviderClient(configuration, context);
            _requestContext = context;

            Metadata = new DataProviderMetadata
            {
                ProviderName = "Http"
            };
        }

        #region GET Requests

        #region Anonymous GET request
        /// <inheritdoc/>
        public async Task<T> SendGetRequestAnonymousAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {            
            return await SendGetRequestAsync<T>(requestUri, additionalHeaders, authOptions:null, operationName, cancellationToken).ConfigureAwait(true);
        }
        #endregion

        #region GET requests with client cert based auth
        /// <inheritdoc/>
        public async Task<T> SendGetRequestCertAuthDefaultAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpProviderAuthenticationOption defaultCertAuthOption = new HttpProviderAuthenticationOption(
                authenticationScheme: HttpProviderAuthenticationSchemes.ClientCert,
                httpProviderAuthConfiguration: new HttpProviderCertAuthConfiguration(_config.DefaultClientCertAuthSubjectName)
                );
            return await SendGetRequestAsync<T>(requestUri, additionalHeaders, defaultCertAuthOption, operationName, cancellationToken).ConfigureAwait(true);
        }

        /// <inheritdoc/>
        public async Task<T> SendGetRequestCertAuthCustomAsync<T>(Uri requestUri, string certificateSubjectName, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(certificateSubjectName))
            {
                throw new ArgumentNullException(paramName:nameof(certificateSubjectName), message: "Please supply a certificate subject name to lookup and attach to the request. To use the default certificate, consider using the SendGetRequestDefaultCertAuthAsync<T> method instead.");
            }

            HttpProviderAuthenticationOption customCertAuthOption = new HttpProviderAuthenticationOption(
                authenticationScheme: HttpProviderAuthenticationSchemes.ClientCert,
                httpProviderAuthConfiguration: new HttpProviderCertAuthConfiguration(certificateSubjectName)
                );            
            return await SendGetRequestAsync<T>(requestUri, additionalHeaders, customCertAuthOption, operationName, cancellationToken).ConfigureAwait(true);
        }
        #endregion

        #region GET requests with aad token based auth

        /// <inheritdoc/>
        public async Task<T> SendGetRequestAadAuthDefaultAsync<T>(Uri requestUri, string audienceWithScope, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpProviderAuthenticationOption defaultAadAuthOption = new HttpProviderAuthenticationOption(
                authenticationScheme: HttpProviderAuthenticationSchemes.AADToken,
                httpProviderAuthConfiguration: new HttpProviderAADTokenAuthConfiguration(
                    _config.DefaultTokenRequestorCertSubjectName,
                    _config.DefaultAADClientId,
                    _config.DefaultAADAuthorityUri,
                    audienceWithScope)
                );            
            return await SendGetRequestAsync<T>(requestUri, additionalHeaders, defaultAadAuthOption, operationName, cancellationToken).ConfigureAwait(true);
        }

        /// <inheritdoc/>
        public async Task<T> SendGetRequestAadAuthCustomAsync<T>(Uri requestUri, string certificateSubjectName, string aadAppClientId, Uri aadAuthorityDomainUri, string audienceWithScope, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpProviderAuthenticationOption customAadAuthOption = new HttpProviderAuthenticationOption(
                authenticationScheme: HttpProviderAuthenticationSchemes.AADToken,
                httpProviderAuthConfiguration: new HttpProviderAADTokenAuthConfiguration(
                    certificateSubjectName,
                    aadAppClientId,
                    aadAuthorityDomainUri,
                    audienceWithScope)
                );            
            return await SendGetRequestAsync<T>(requestUri, additionalHeaders, customAadAuthOption, operationName, cancellationToken).ConfigureAwait(true);
        }
        #endregion

        /// <inheritdoc/>
        public async Task<T> SendGetRequestAsync<T>(Uri requestUri, Dictionary<string, string> additionalHeaders = null, HttpProviderAuthenticationOption authOptions = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            AddHttpCallToMetadata(new HttpCallInfo()
            {
                Text = $@"GET {requestUri}
                        {Environment.NewLine}Authentication: {GetAuthSchemeMetadaString(authOptions)}
                        {Environment.NewLine}{GetHeadersString(additionalHeaders)}",
                OperationName = operationName
            });
            return await _httpDataProviderClient.MakeHttpGetRequestAsync<T>(requestUri, additionalHeaders, authOptions, cancellationToken).ConfigureAwait(true);
        }

        #endregion

        #region POST requests

        #region Anonymous POST request
        /// <inheritdoc/>
        public async Task<T> SendPostRequestAnonymousAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            return await SendPostRequestAsync<T>(requestUri, jsonPostBody, additionalHeaders, authOptions: null, operationName, cancellationToken).ConfigureAwait(true);
        }
        #endregion 

        #region POST requests with client cert based auth
        /// <inheritdoc/>
        public async Task<T> SendPostRequestCertAuthDefaultAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpProviderAuthenticationOption defaultCertAuthOption = new HttpProviderAuthenticationOption(
               authenticationScheme: HttpProviderAuthenticationSchemes.ClientCert,
               httpProviderAuthConfiguration: new HttpProviderCertAuthConfiguration(_config.DefaultClientCertAuthSubjectName)
               );
            return await SendPostRequestAsync<T>(requestUri, jsonPostBody, additionalHeaders, defaultCertAuthOption, operationName, cancellationToken).ConfigureAwait(true);
        }

        /// <inheritdoc/>
        public async Task<T> SendPostRequestCertAuthCustomAsync<T>(Uri requestUri, string jsonPostBody, string certificateSubjectName, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(certificateSubjectName))
            {
                throw new ArgumentNullException(paramName: nameof(certificateSubjectName), message: "Please supply a certificate subject name to lookup and attach to the request. To use the default certificate, consider using the SendGetRequestDefaultCertAuthAsync<T> method instead.");
            }

            HttpProviderAuthenticationOption customCertAuthOption = new HttpProviderAuthenticationOption(
                authenticationScheme: HttpProviderAuthenticationSchemes.ClientCert,
                httpProviderAuthConfiguration: new HttpProviderCertAuthConfiguration(certificateSubjectName)
                );
            return await SendPostRequestAsync<T>(requestUri, jsonPostBody, additionalHeaders, customCertAuthOption, operationName, cancellationToken).ConfigureAwait(true);
        }
        #endregion

        #region POST requests with aad token based auth
        /// <inheritdoc/>
        public async Task<T> SendPostRequestAadAuthDefaultAsync<T>(Uri requestUri, string jsonPostBody, string audienceWithScope, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpProviderAuthenticationOption defaultAadAuthOption = new HttpProviderAuthenticationOption(
                authenticationScheme: HttpProviderAuthenticationSchemes.AADToken,
                httpProviderAuthConfiguration: new HttpProviderAADTokenAuthConfiguration(
                    _config.DefaultTokenRequestorCertSubjectName,
                    _config.DefaultAADClientId,
                    _config.DefaultAADAuthorityUri,
                    audienceWithScope)
                );

            return await SendPostRequestAsync<T>(requestUri, jsonPostBody, additionalHeaders, defaultAadAuthOption, operationName, cancellationToken).ConfigureAwait(true);
        }

        /// <inheritdoc/>
        public async Task<T> SendPostRequestAadAuthCustomAsync<T>(Uri requestUri, string jsonPostBody, string certificateSubjectName, string aadAppClientId, Uri aadAuthorityDomainUri, string audienceWithScope, Dictionary<string, string> additionalHeaders = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpProviderAuthenticationOption customAadAuthOption = new HttpProviderAuthenticationOption(
                authenticationScheme: HttpProviderAuthenticationSchemes.AADToken,
                httpProviderAuthConfiguration: new HttpProviderAADTokenAuthConfiguration(
                    certificateSubjectName,
                    aadAppClientId,
                    aadAuthorityDomainUri,
                    audienceWithScope)
                );

            return await SendPostRequestAsync<T>(requestUri, jsonPostBody, additionalHeaders, customAadAuthOption, operationName, cancellationToken).ConfigureAwait(true);
        }
        #endregion

        /// <inheritdoc/>
        public async Task<T> SendPostRequestAsync<T>(Uri requestUri, string jsonPostBody, Dictionary<string, string> additionalHeaders = null, HttpProviderAuthenticationOption authOptions = null, string operationName = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            AddHttpCallToMetadata(new HttpCallInfo()
            {
                Text = $@"POST {requestUri}
                        {Environment.NewLine}Authentication: {GetAuthSchemeMetadaString(authOptions)}
                        {Environment.NewLine}{GetHeadersString(additionalHeaders)}
                        {Environment.NewLine}Request body:{Environment.NewLine}{jsonPostBody}",
                OperationName = operationName
            });

            return await _httpDataProviderClient.MakeHttpPostRequestAsync<T>(requestUri, jsonPostBody, additionalHeaders = null, authOptions, cancellationToken).ConfigureAwait(true);
        }
        #endregion

        private string GetAuthSchemeMetadaString(HttpProviderAuthenticationOption authOptions = null)
        {
            string authSchemeName = "Anonymous.";
            if (authOptions != null)
            {
                authSchemeName = (
                    authOptions.CertificateSubjectName.Equals(_config.DefaultClientCertAuthSubjectName, StringComparison.OrdinalIgnoreCase)
                    || authOptions.CertificateSubjectName.Equals(_config.DefaultTokenRequestorCertSubjectName, StringComparison.OrdinalIgnoreCase)
                    ) ? "Default" : "Custom";
                switch (authOptions.AuthenticationScheme)
                {
                    case HttpProviderAuthenticationSchemes.ClientCert:
                        authSchemeName += " client certificate.";
                        break;
                    case HttpProviderAuthenticationSchemes.AADToken:
                        authSchemeName += " AAD app bearer token.";
                        break;
                    default:
                        authSchemeName = "Anonymous.";
                        break;
                }
            }
            return authSchemeName;
        }
        
        private string GetHeadersString(Dictionary<string, string> additionalHeaders = null)
        {
            if (additionalHeaders != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Additional headers:");
                foreach (var header in additionalHeaders)
                {
                    sb.AppendLine($"+ {header.Key}: {header.Value}");
                }
                return sb.ToString();
            }
            return string.Empty;
        }

        private void AddHttpCallToMetadata(HttpCallInfo call)
        {
            if(call != null)
            {
                bool callAlreadyAdded = false;
                callAlreadyAdded = Metadata.PropertyBag.Any(x => x.Key == "Query" 
                                                            && x.Value.GetType() == typeof(HttpCallInfo) 
                                                            && x.Value.CastTo<HttpCallInfo>().Text.Equals(call.Text, StringComparison.OrdinalIgnoreCase));
                if(!callAlreadyAdded)
                {
                    Metadata.PropertyBag.Add(new KeyValuePair<string, object>("Query", call));
                }
            }
        }

        public DataProviderMetadata GetMetadata()
        {
            return Metadata;
        }
    }
}
