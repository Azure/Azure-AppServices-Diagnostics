using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.RuntimeHost.Models
{
    public class RuntimeContext<TResource>
        where TResource: IResource
    {
        public bool ClientIsInternal { get; set; }

        public OperationContext<TResource> OperationContext;
    }
}
