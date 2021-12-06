using Diagnostics.Logger;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.TokenService
{
    public abstract class LogAnalyticsTokenServiceBase
    {
        protected abstract string workspaceId { get; set; }
        protected abstract string clientId { get; set; }
        protected abstract string clientSecret { get; set; }

        protected abstract string domain { get; set; }
        protected abstract string authEndpoint { get; set; }
        protected abstract string tokenAudience { get; set; }

        protected abstract ActiveDirectoryServiceSettings adSettings { get; set; }
        protected abstract Microsoft.Rest.ServiceClientCredentials creds { get; set; }

        protected abstract OperationalInsightsDataClient client { get; set; }


        protected abstract string TokenServiceName { get; set; }

        public Microsoft.Rest.ServiceClientCredentials getCreds()
        {
            return creds;
        }

        public string getWorkspaceId()
        {
            return workspaceId;
        }

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
                    await Authenticate();
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
      
        private async Task Authenticate()
        {
            adSettings = new ActiveDirectoryServiceSettings
            {
                AuthenticationEndpoint = new Uri(authEndpoint),
                TokenAudience = new Uri(tokenAudience),
                ValidateAuthority = true
            };

            creds = await ApplicationTokenProvider.LoginSilentAsync(domain, clientId, clientSecret, adSettings).ConfigureAwait(false);
        }
    }
}
