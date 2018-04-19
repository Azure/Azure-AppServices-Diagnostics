using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Utilities
{
    public class UriElements
    {
        public const string HealthPing = "/healthping";

        public const string WebResourceProviderName = "Microsoft.Web";
        public const string NetworkResourceProviderName = "Microsoft.Network";

        public const string ResourceRoot = "subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/" + WebResourceProviderName;
        public const string SitesResource = ResourceRoot + "/sites/{siteName}";
        public const string HostingEnvironmentResource = ResourceRoot + "/hostingEnvironments/{hostingEnvironmentName}";

        public const string Diagnostics = "/diagnostics";

        public const string Query = "query";
        public const string Detectors = "detectors";
        public const string DetectorResource = "/{detectorId}";
    }
}
