using System.Data.SqlTypes;
using System.Text;
using System.Text.RegularExpressions;

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

        public static string GetResourceProviderFromUri(string armUri)
        {
          
            if(armUri.StartsWith('/'))
            {
                armUri = armUri.TrimStart('/');
            }
            string[] splits = armUri.Split('/');
            StringBuilder sb = new StringBuilder();
            sb.Append(splits[5]);
            sb.Append('/');
            sb.Append(splits[6]);
            return sb.ToString();
        }
    }
}
