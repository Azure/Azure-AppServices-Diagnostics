using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using System;
using System.Linq;
using Diagnostics.Logger;

namespace Diagnostics.DataProviders.TokenService
{
    public class TokenServiceBase
    {
        private Task<AuthenticationResult> acquireTokenTask;

        /// <summary>
        /// Class used to retreive auth tokens from AAD.
        /// </summary>
        public AuthenticationContext AuthenticationContext;

        /// <summary>
        /// AAD Client credentials that include client id and secret.
        /// </summary>
        public ClientCredential ClientCredential;

        /// <summary>
        /// AAD Resource.
        /// </summary>
        public string Resource;

        /// <summary>
        /// Token Service name used for logging to Kusto.
        /// </summary>
        public string TokenServiceName;

        /// <summary>
        /// Checks if token has been acquired atleast once.
        /// </summary>
        public bool TokenAcquiredAtleastOnce;

        /// <summary>
        /// AAD issued auth token.
        /// </summary>
        public string AuthorizationToken { get; private set; }

        /// <summary>
        /// Acquires Security Token from AAD Authority for the given <see cref="ClientCredential"/> and <see cref="Resource"/>.
        /// </summary>
        public async Task StartTokenRefresh()
        {
            while (true)
            {
                DateTime invocationStartTime = DateTime.UtcNow;
                string exceptionType = string.Empty;
                string exceptionDetails = string.Empty;
                string message = string.Empty;

                try
                {
                    var items = AuthenticationContext.TokenCache.ReadItems();
                    var tokenServiceCacheItems = items.FirstOrDefault(x => x.Resource == Resource);
                    if (tokenServiceCacheItems != null)
                    {
                        AuthenticationContext.TokenCache.DeleteItem(tokenServiceCacheItems);
                    }

                    acquireTokenTask = AuthenticationContext.AcquireTokenAsync(Resource, ClientCredential);
                    AuthenticationResult authResult = await acquireTokenTask;
                    AuthorizationToken = GetAuthTokenFromAuthenticationResult(authResult);
                    TokenAcquiredAtleastOnce = true;
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
                    DiagnosticsETWProvider.Instance.LogTokenRefreshSummary(
                        TokenServiceName,
                        message,
                        latencyInMs,
                        invocationStartTime.ToString("HH:mm:ss.fff"),
                        invocationEndTime.ToString("HH:mm:ss.fff"),
                        exceptionType,
                        exceptionDetails);
                }

                await Task.Delay(DataProviderConstants.TokenRefreshIntervalInMs);
            }
        }

        /// <summary>
        /// Gets AAD issued auth token.
        /// </summary>
        public async Task<string> GetAuthorizationTokenAsync()
        {
            if (!TokenAcquiredAtleastOnce)
            {
                var authResult = await acquireTokenTask;
                return GetAuthTokenFromAuthenticationResult(authResult);
            }

            return AuthorizationToken;
        }

        private string GetAuthTokenFromAuthenticationResult(AuthenticationResult authenticationResult)
        {
            return $"{authenticationResult.AccessTokenType} {authenticationResult.AccessToken}";
        }
    }
}
