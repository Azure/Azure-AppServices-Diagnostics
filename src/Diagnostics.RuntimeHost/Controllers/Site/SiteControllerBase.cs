using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Controllers
{
    public abstract class SiteControllerBase : DiagnosticControllerBase<App>
    {
        protected ISiteService _siteService;

        public SiteControllerBase(IStampService stampService, ISiteService siteService, ICompilerHostClient compilerHostClient, ISourceWatcherService sourceWatcherService, IInvokerCacheService invokerCache, IDataSourcesConfigurationService dataSourcesConfigService)
            : base(stampService, compilerHostClient, sourceWatcherService, invokerCache, dataSourcesConfigService)
        {
            this._siteService = siteService;
        }

        protected async Task<App> GetAppResource(string subscriptionId, string resourceGroup, string appName, DiagnosticSiteData postBody, DateTime startTime, DateTime endTime)
        {
            string requestId = string.Empty;
            if (this.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues requestIds))
            {
                requestId = requestIds.FirstOrDefault() ?? string.Empty;
            }

            App app = new App(subscriptionId, resourceGroup, appName)
            {
                DefaultHostName = postBody.DefaultHostName,
                Hostnames = postBody.HostNames != null ? postBody.HostNames.Select(p => p.Name) : new List<string>(),
                WebSpace = postBody.WebSpace,
                ScmSiteHostname = postBody.ScmSiteHostname,
                Stamp = await GetHostingEnvironment(postBody.Stamp.Subscription, postBody.Stamp.ResourceGroup, postBody.Stamp != null ? postBody.Stamp.Name : string.Empty, postBody.Stamp, startTime, endTime),
                AppType = GetApplicationType(postBody.Kind),
                PlatformType = (!string.IsNullOrWhiteSpace(postBody.Kind) && postBody.Kind.ToLower().Contains("linux")) ? PlatformType.Linux : PlatformType.Windows,
                StackType = await this._siteService.GetApplicationStack(subscriptionId, resourceGroup, appName, requestId)
            };

            switch (app.Stamp.HostingEnvironmentType)
            {
                case HostingEnvironmentType.V1:
                    app.StampType = StampType.ASEV1;
                    break;
                case HostingEnvironmentType.V2:
                    app.StampType = StampType.ASEV2;
                    break;
                default:
                    app.StampType = StampType.Public;
                    break;
            }

            return app;
        }

        private AppType GetApplicationType(string kind)
        {
            if (string.IsNullOrWhiteSpace(kind)) return AppType.WebApp;

            string kindProperty = kind.ToLower();

            if (kindProperty.Contains("api")) return AppType.ApiApp;
            else if (kindProperty.Contains("function")) return AppType.FunctionApp;
            else if (kindProperty.Contains("mobile")) return AppType.MobileApp;
            else if (kindProperty.Contains("gateway")) return AppType.GatewayApp;
            else return AppType.WebApp;
        }
    }
}
