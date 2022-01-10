namespace Diagnostics.RuntimeHost.Utilities
{
    public class UriElements
    {
        public const string HealthPing = "/healthping";

        public const string ResourceProvidersRoot = "subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/";

        #region Microsoft.Web Urls

        public const string WebResourceRoot = ResourceProvidersRoot + ResourceProviders.Web;
        public const string SitesResource = WebResourceRoot + "/sites/{siteName}";
        public const string ContainerAppResource = WebResourceRoot + "/containerApps/{siteName}";
        public const string HostingEnvironmentResource = WebResourceRoot + "/hostingEnvironments/{hostingEnvironmentName}";

        #endregion Microsoft.Web Urls

        #region Microsoft.Cert and Domain Urls

        public const string AppServiceCertResource = ResourceProvidersRoot + ResourceProviders.AppServiceCert + "/certificateOrders/{certificateName}";
        public const string AppServiceDomainResource = ResourceProvidersRoot + ResourceProviders.AppServiceDomain + "/domains/{domainName}";

        #endregion Microsoft.Cert and Domain Urls

        #region Other Resource Urls

        public const string LogicAppResource = ResourceProvidersRoot + ResourceProviders.LogicApp + "/workflows/{logicAppName}";
        public const string ApiManagementServiceResource = ResourceProvidersRoot + ResourceProviders.ApiManagement + "/service/{serviceName}";
        public const string AzureKubernetesServiceResource = ResourceProvidersRoot + ResourceProviders.AzureKubernetesService + "/managedClusters/{clusterName}";
        public const string ArmResource = ResourceProvidersRoot + "{provider}/{resourceTypeName}/{resourceName}";

        #endregion Other Resource Urls

        public const string Query = "diagnostics/query";
        public const string Publish = "diagnostics/publish";
        public const string Detectors = "detectors";
        public const string DetectorResource = "/{detectorId}";
        public const string Gists = "gists";
        public const string GistResource = "/{gistId}";
        public const string Insights = "insights";
        public const string InsightResource = "/{insightId}";
        public const string Statistics = "/statistics";
        public const string StatisticsResource = "/{invokerId}";
        public const string StatisticsQuery = "/statisticsQuery";
        public const string AppStack = "appstack";
        public const string Configurations = "configurations";
        public const string KustoClusterMappings = Configurations + "/kustoClusterMappings";
        public const string UniqueResourceId = "/{id}";
        public const string DiagnosticReport = "diagnostics";

        // Constants for internal api interactions
        public const string Internal = "internal";

        public const string Logger = "logger";
        public const string PublishModel = "publishmodel";
        public const string TrainModel = "trainmodel";
        public const string RefreshModel = "refreshmodel";
        public const string UpdateResourceConfig = "updateResourceConfig";
        public const string PassThroughAPIRoute = "/api/invoke";

        #region DevOps Urls

        public const string DevOps = "devops";
        public const string DevOpsMakePR = "makePR";
        public const string DevOpsPush = "push";
        public const string DevOpsGetCode = "getCode";
        public const string DevOpsGetBranches = "getBranches";
        public const string DevOpsConfig = "devopsConfig";
        #endregion
    }

    /// <summary>
    /// List of Resource Providers
    /// </summary>
    public class ResourceProviders
    {
        public const string Web = "Microsoft.Web";
        public const string AppServiceCert = "Microsoft.CertificateRegistration";
        public const string AppServiceDomain = "Microsoft.DomainRegistration";
        public const string LogicApp = "Microsoft.Logic";
        public const string ApiManagement = "Microsoft.ApiManagement";
        public const string AzureKubernetesService = "Microsoft.ContainerService";
    }
}
