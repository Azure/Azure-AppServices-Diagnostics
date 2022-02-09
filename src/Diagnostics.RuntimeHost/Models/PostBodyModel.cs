using System;
using System.Collections.Generic;

namespace Diagnostics.RuntimeHost.Models
{
    public class DiagnosticSiteData
    {
        public string Name { get; set; }
        public string Tags { get; set; }
        public string Kind { get; set; }
        public string Stack { get; set; }
        public string NamespaceDescriptor { get; set; }
        public bool IsDefaultContainer { get; set; }
        public bool? IsLinux { get; set; }
        public bool? IsXenon { get; set; }
        public string DefaultHostName { get; set; }
        public string ScmSiteHostname { get; set; }
        public DateTime? ModifiedTimeUtc { get; set; }
        public string WebSpace { get; set; }
        public IEnumerable<DiagnosticHostnameData> HostNames { get; set; }
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
        public string InternalName { get; set; }
        public string ServiceAddress { get; set; }
        public int State { get; set; }
        public string DnsSuffix { get; set; }
        public DiagnosticStampType Kind { get; set; }
        public string Tags { get; set; }
        public DateTime? UnhealthySince { get; set; }
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
        ASEV2,
        ASEV3
    }

    public class DiagnosticReportQuery
    {
        public string Text { get; set; }
        public List<string> Detectors { get; set; }
        public string SupportTopicId { get; set; }
        public string SapSupportTopicId { get; set; }
    }

    public class BodyValidationResult
    {
        public bool Status { get; set; }
        public string Message { get; set; }
    }

    public class DiagnosticContainerAppData
    {
        public string ContainerAppName { get; set; }
        public string Fqdn { get; set; }
        public string Location { get; set; }
        public string Tags { get; set; }
        public string ResourceGroupName { get; set; }
        public string SubscriptionName { get; set; }
        public string KubeEnvironmentName { get; set; }
        public string GeoMasterName { get; set; }
        public string ServiceAddress { get; set; }
        public string Kind { get; set; }
        public bool IsInAppNamespace { get; set; }
    }
}
