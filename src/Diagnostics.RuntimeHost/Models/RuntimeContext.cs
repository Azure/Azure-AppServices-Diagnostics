using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.RuntimeHost.Models
{
    public class RuntimeContext<TResource> : IRuntimeContext<TResource> where TResource : IResource
    {
        public bool ClientIsInternal { get; set; }

        public OperationContext<TResource> OperationContext { get; set; }
        public string CloudDomain { get; }

        public RuntimeContext(IConfiguration configuration)
        {
            CloudDomain = string.IsNullOrWhiteSpace(configuration.GetValue<string>("CloudDomain")) ? DataProviderConstants.AzureCloud : configuration.GetValue<string>("CloudDomain");
        }
    }

    public interface IRuntimeContext<TResource> where TResource : IResource
    {
        bool ClientIsInternal { get; set; }
        string CloudDomain { get; }
        OperationContext<TResource> OperationContext { get; set; }
    }
}
