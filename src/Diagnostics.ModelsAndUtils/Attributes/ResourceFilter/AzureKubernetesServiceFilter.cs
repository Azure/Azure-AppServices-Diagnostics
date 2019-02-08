using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    public class AzureKubernetesServiceFilter : ResourceFilterBase
    {
        public AzureKubernetesServiceFilter(bool internalOnly = true) : base(ResourceType.AzureKubernetesService, internalOnly)
        {
        }
    }
}
