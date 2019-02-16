using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Models
{
    public class DiagnosticSiteData
    {
        public string Name { get; set; }

        public string Tags { get; set; }

        public string Kind { get; set; }

        public string Stack { get; set; }

        [JsonProperty("namespace_descriptor")]
        public string NamespaceDescriptor { get; set; }

        [JsonProperty("is_default_container")]
        public bool IsDefaultContainer { get; set; }

        [JsonProperty("is_linux")]
        public bool? IsLinux { get; set; }

        [JsonProperty("default_host_name")]
        public string DefaultHostName { get; set; }

        [JsonProperty("scm_site_hostname")]
        public string ScmSiteHostname { get; set; }

        [JsonProperty("modified_time_utc")]
        public DateTime? ModifiedTimeUtc { get; set; }

        [JsonProperty("web_space")]
        public string WebSpace { get; set; }

        [JsonProperty("host_names")]
        public IEnumerable<DiagnosticHostnameData> HostNames { get; set; }

        [JsonProperty("resource_group")]
        public string ResourceGroup { get; set; }


        public DiagnosticStampData Stamp { get; set; }
    }

    public class DiagnosticHostnameData
    {
        public string Name { get; set; }
        public int Type { get; set; }

    }

    public class DiagnosticStampData
    {
        public string Name { get; set; }

        [JsonProperty("internal_name")]
        public string InternalName { get; set; }

        [JsonProperty("service_address")]
        public string ServiceAddress { get; set; }

        public int State { get; set; }

        [JsonProperty("dns_suffix")]
        public string DnsSuffix { get; set; }

        public DiagnosticStampType Kind { get; set; }

        public string Tags { get; set; }

        [JsonProperty("unhealthy_since")]
        public DateTime? UnhealthySince { get; set; }

        [JsonProperty("suspended_on")]
        public DateTime? SuspendedOn { get; set; }

        public bool IsUnhealthy
        {
            get
            {
                return UnhealthySince != null;
            }
        }

        public string Location { get; set; }
        public string Subscription { get; set; }
        public string ResourceGroup { get; set; }
    }

    public enum DiagnosticStampType
    {
        Stamp,
        ASEV1,
        ASEV2
    }
}
