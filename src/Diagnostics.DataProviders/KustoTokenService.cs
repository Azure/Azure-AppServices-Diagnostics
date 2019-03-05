using Diagnostics.Logger;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class KustoTokenService
    {
        private AuthenticationContext _authContext;
        private ClientCredential _clientCredential;
        private KustoDataProviderConfiguration _configuration;
        private static readonly Lazy<KustoTokenService> _instance = new Lazy<KustoTokenService>(() => new KustoTokenService());
        private string _authorizationToken;
        private bool _tokenAcquiredAtleastOnce;
        private Task<AuthenticationResult> _acquireTokenTask;

        public static KustoTokenService Instance => _instance.Value;

        public string AuthorizationToken => _authorizationToken;

        private KustoTokenService() : base()
        {
        }

        public void Initialize(KustoDataProviderConfiguration configuration)
        {
            _configuration = configuration;
            _authContext = new AuthenticationContext(_configuration.AADAuthority);
            _clientCredential = new ClientCredential(_configuration.ClientId, _configuration.AppKey);
            _tokenAcquiredAtleastOnce = false;
            StartTokenRefresh();
        }

        private async Task StartTokenRefresh()
        {
            while (true)
            {
                DateTime invocationStartTime = DateTime.UtcNow;
                string exceptionType = string.Empty;
                string exceptionDetails = string.Empty;
                string message = string.Empty;

                try
                {
                    var items = _authContext.TokenCache.ReadItems();
                    var kustoCacheItem = items.FirstOrDefault(x => x.Resource == _configuration.AADKustoResource);
                    if (kustoCacheItem != null)
                    {
                        _authContext.TokenCache.DeleteItem(kustoCacheItem);
                    }

                    _acquireTokenTask = _authContext.AcquireTokenAsync(_configuration.AADKustoResource, _clientCredential);
                    AuthenticationResult authResult = await _acquireTokenTask;
                    _authorizationToken = GetAuthTokenFromAuthenticationResult(authResult);
                    _tokenAcquiredAtleastOnce = true;
                    message = "Token Acquisition Status : Success";
                }
                catch (Exception ex)
                {
                    exceptionType = ex.GetType().ToString();
                    exceptionDetails = ex.ToString();
                    message = "Token Acquisition Status : Failed";
                }
                finally
                {
                    DateTime invocationEndTime = DateTime.UtcNow;
                    long latencyInMs = Convert.ToInt64((invocationEndTime - invocationStartTime).TotalMilliseconds);
                    DiagnosticsETWProvider.Instance.LogKustoTokenRefreshSummary(
                        "KustoTokenRefreshService",
                        message,
                        latencyInMs,
                        invocationStartTime.ToString("HH:mm:ss.fff"),
                        invocationEndTime.ToString("HH:mm:ss.fff"),
                        exceptionType,
                        exceptionDetails
                        );
                }

                await Task.Delay(DataProviderConstants.TokenRefreshIntervalInMs);
            }
        }

        private string GetAuthTokenFromAuthenticationResult(AuthenticationResult authenticationResult)
        {
            return $"{authenticationResult.AccessTokenType} {authenticationResult.AccessToken}";
        }

        public async Task<string> GetAuthorizationTokenAsync()
        {
            if (!_tokenAcquiredAtleastOnce)
            {
                var authResult = await _acquireTokenTask;
                return GetAuthTokenFromAuthenticationResult(authResult);
            }

            return _authorizationToken;
        }
    }
}
