using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Class representing Filter for App service certificate
    /// </summary>
    public class AppServiceCertificateFilter : ResourceFilterBase
    {
        public AppServiceCertificateFilter(bool internalOnly = true) : base(ResourceType.AppServiceCertificate, internalOnly)
        {
        }
    }
}
