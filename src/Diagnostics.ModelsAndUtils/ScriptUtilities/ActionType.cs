namespace Diagnostics.ModelsAndUtils.ScriptUtilities
{
    /// <summary>
    /// Actions that a Solution can perform, such as ARM API requests.
    /// </summary>
    public enum ActionType
    {
        RestartSite,
        UpdateSiteAppSettings,
        KillW3wpOnInstance,
        AzureApiRequest
    }
}
