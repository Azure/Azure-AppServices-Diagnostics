using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.ModelsAndUtils.ScriptUtilities
{
    /// <summary>
    /// Build URIs to interact with Azure resources.
    /// </summary>
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

            var baseUri = new Uri(parts[0]);

            var extensions = "";
            foreach (var part in parts.Skip(1))
            {
                extensions = $"{extensions.TrimEnd('/')}/{part.Trim('/')}";
            }

            return new Uri(baseUri, extensions).ToString();
        }

        public static string BuildDetectorLink(string resourceUri, string detectorId)
        {
            return BuildUri(
                "https://ms.portal.azure.com",
                $"/?websitesextension_ext=asd.featurePath%3Ddetectors%2F{detectorId}#@microsoft.onmicrosoft.com",
                "/resource/",
                resourceUri,
                "/customtroubleshoot"
            );
        }

        public static Uri BuildDetectorUri(string resourceUri, string detectorId)
        {
            return new Uri(BuildDetectorLink(resourceUri, detectorId));
        }
    }
}
