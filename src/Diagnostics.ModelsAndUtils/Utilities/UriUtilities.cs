using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.ModelsAndUtils.Utilities
{
    public static class UriUtilities
    {
        public static string BuildResourceUri(string subscriptionId, string resourceGroup, string resourceName,
            string provider = "Microsoft.Web", string resourceTypeName = "sites")
        {
            return $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/" +
                $"providers/{provider}/{resourceTypeName}/{resourceName}";
        }
    }
}
