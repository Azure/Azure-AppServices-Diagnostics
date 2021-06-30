using System;
using Microsoft.Extensions.Configuration;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Diagnostics.RuntimeHost.Services
{
    public static class DevopsClientFactory
    {
        public static GitHttpClient GetDevopsClient(PartnerConfig partnerConfig, IConfiguration configuration)
        {
            string password = configuration["pat"];
            Uri baseUri = new Uri(partnerConfig.DevOpsUrl);
            string project = partnerConfig.Project;
            string repoName = partnerConfig.Repository;
            var credentials = (VssCredentials)(FederatedCredential)new VssBasicCredential("pat", password);
            var connection = new VssConnection(baseUri, credentials);
            return connection.GetClient<GitHttpClient>();
        }
    }
}
