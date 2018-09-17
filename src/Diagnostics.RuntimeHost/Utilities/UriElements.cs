using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Utilities
{
    public class UriElements
    {
        public const string HealthPing = "/healthping";

        public const string ResourceProvidersRoot = "subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/";

        #region Microsoft.Web Urls

        public const string WebResourceRoot = ResourceProvidersRoot + ResourceProviders.Web;
        public const string SitesResource = WebResourceRoot + "/sites/{siteName}";
        public const string HostingEnvironmentResource = WebResourceRoot + "/hostingEnvironments/{hostingEnvironmentName}";

        #endregion

        #region Microsoft.Cert and Domain Urls

        public const string AppServiceCertResource = ResourceProvidersRoot + ResourceProviders.AppServiceCert + "/certificateOrders/{certificateName}";
        public const string AppServiceDomainResource = ResourceProvidersRoot + ResourceProviders.AppServiceDomain + "/domains/{domainName}";

        #endregion

        #region Other Resource Urls

        public const string LogicAppResource = ResourceProvidersRoot + ResourceProviders.LogicApp + "/workflows/{logicAppName}";
        public const string ApiManagementServiceResource = ResourceProvidersRoot + ResourceProviders.ApiManagement + "/service/{serviceName}";

        #endregion
        
        public const string Query = "diagnostics/query";
        public const string Publish = "diagnostics/publish";
        public const string Detectors = "detectors";
        public const string DetectorResource = "/{detectorId}";
        public const string Insights = "insights";
        public const string InsightResource = "/{insightId}";
        public const string Statistics = "/statistics";
        public const string StatisticsResource = "/{invokerId}";
        public const string StatisticsQuery = "/statisticsQuery";
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
        
    }
}
