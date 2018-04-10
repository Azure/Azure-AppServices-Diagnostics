using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public interface IResource
    {
    }

    public abstract class Resource : IResource
    {
        public string SubscriptionId;

        public string ResourceGroup;

        public IEnumerable<string> TenantIdList;
    }

    public sealed class SiteResource : Resource
    {
        public string SiteName;

        public IEnumerable<string> HostNames;

        public string Stamp;

        public string SourceMoniker {
            get
            {
                return string.IsNullOrWhiteSpace(Stamp) ? Stamp.ToUpper().Replace("-", string.Empty) : string.Empty;
            }
        }
    }
}
