using System;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Diagnostics.DataProviders
{
    public class TokenRefresher
    {
        private Task<AuthenticationResult> _acquireTokenTask;
        private bool tokenAcquiredAtleastOnce;

        public TokenRefresher(string aadAuthority, string clientId, string appKey, string aadResource, string tokenServiceName)
        {
            AuthenticationContext = new AuthenticationContext(aadAuthority);
            ClientCredential = new ClientCredential(clientId, appKey);
            Resource = aadResource;
            TokenServiceName = tokenServiceName;

            Task.Run(async () =>
            {
                await StartTokenRefresh().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets AAD issued auth token.
        /// </summary>
        public string AuthorizationToken { get; private set; }

        /// <summary>
        /// Gets or sets class used to retreive auth tokens from AAD.
        /// </summary>
        public AuthenticationContext AuthenticationContext { get; set; }

        /// <summary>
        /// Gets or sets AAD Client credentials that include client id and secret.
        /// </summary>
        public ClientCredential ClientCredential { get; set; }

        /// <summary>
        /// Gets or sets AAD Resource.
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets token service name used for logging to Kusto.
        /// </summary>
        public string TokenServiceName { get; set; }

        public async Task<string> PullNewTokenAsync()
        {
            var items = AuthenticationContext.TokenCache.ReadItems();
            var tokenServiceCacheItems = items.FirstOrDefault(x => x.Resource == Resource);
            if (tokenServiceCacheItems != null)
            {
                AuthenticationContext.TokenCache.DeleteItem(tokenServiceCacheItems);
            }

            _acquireTokenTask = AuthenticationContext.AcquireTokenAsync(Resource, ClientCredential);
            AuthenticationResult authResult = await _acquireTokenTask;
            AuthorizationToken = GetAuthTokenFromAuthenticationResult(authResult);

            return AuthorizationToken;
        }

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
                    await PullNewTokenAsync();

                    tokenAcquiredAtleastOnce = true;
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
            if (!tokenAcquiredAtleastOnce)
            {
                if (_acquireTokenTask == null)
                {
                    return await PullNewTokenAsync().ConfigureAwait(false);
                }

                var authResult = await _acquireTokenTask;
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
