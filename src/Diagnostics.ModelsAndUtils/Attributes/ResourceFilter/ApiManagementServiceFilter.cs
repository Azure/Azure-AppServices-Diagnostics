using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Class representing Filter for API Management Service
    /// </summary>
    public class ApiManagementServiceFilter : ResourceFilterBase
    {
        public ApiManagementServiceFilter(bool internalOnly = true) : base(ResourceType.ApiManagementService, internalOnly)
        {
        }
    }
}
