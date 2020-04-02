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
    }
}
