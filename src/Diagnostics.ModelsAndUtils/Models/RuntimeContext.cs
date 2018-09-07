using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class RuntimeContext<TResource>
        where TResource: IResource
    {
        public bool ClientIsInternal { get; set; }

        public OperationContext<TResource> OperationContext;
    }
}
