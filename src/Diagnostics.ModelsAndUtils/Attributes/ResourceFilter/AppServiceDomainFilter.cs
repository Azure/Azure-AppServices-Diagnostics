using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Class representing Filter for App service domain
    /// </summary>
    public class AppServiceDomainFilter : ResourceFilterBase
    {
        public AppServiceDomainFilter(bool internalOnly = true) : base(ResourceType.AppServiceDomain, internalOnly)
        {
        }
    }
}
