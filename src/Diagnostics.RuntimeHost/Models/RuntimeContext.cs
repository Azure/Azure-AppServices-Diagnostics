using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.RuntimeHost.Models
{
    public class RuntimeContext<TResource> : IRuntimeContext<TResource> where TResource : IResource
    {
        public bool ClientIsInternal { get; set; }

        public OperationContext<TResource> OperationContext { get; set; }
    }

    public interface IRuntimeContext<TResource> where TResource : IResource
    {
        bool ClientIsInternal { get; set; }
        OperationContext<TResource> OperationContext { get; set; }
    }
}
