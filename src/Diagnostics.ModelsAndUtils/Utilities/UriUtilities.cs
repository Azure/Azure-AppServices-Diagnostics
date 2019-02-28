using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.ModelsAndUtils.Utilities
{
    public static class UriUtilities
    {
        public static string BuildAzureResourceUri(string subscriptionId, string resourceGroup, string resourceName,
            string provider = "Microsoft.Web", string resourceTypeName = "sites")
        {
            return $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/" +
                $"providers/{provider}/{resourceTypeName}/{resourceName}";
        }

        public static string BuildUri(params string[] parts)
        {
            if (!parts.Any())
            {
                return string.Empty;
            }

            var result = new Uri(parts[0]);

            foreach (var part in parts.Skip(1))
            {
                result = new Uri(result, part);
            }

            return result.ToString();
        }

        public static string BuildDetectorLink(string resourceUri, string detectorId)
        {
            return BuildUri(
                "https://ms.portal.azure.com/?websitesextension_ext=asd.featurePath%3D",
                $"detectors%2F{detectorId}#@microsoft.onmicrosoft.com/resource/",
                resourceUri,
                "/customtroubleshoot"
            );
        }
    }
}
