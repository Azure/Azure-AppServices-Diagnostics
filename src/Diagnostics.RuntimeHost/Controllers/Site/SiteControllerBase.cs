using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.Extensions.Primitives;

namespace Diagnostics.RuntimeHost.Controllers
{
    public abstract class SiteControllerBase : DiagnosticControllerBase<App>
    {
        protected ISiteService _siteService;

        public SiteControllerBase(IServiceProvider services, IRuntimeContext<App> runtimeContext)
            : base(services, runtimeContext)
        {
            this._siteService = (ISiteService)services.GetService(typeof(ISiteService));
        }

        protected async Task<App> GetAppResource(string subscriptionId, string resourceGroup, string appName, DiagnosticSiteData postBody, DateTime startTime, DateTime endTime)
        {
            var platformType = GetPlatformType(postBody);

            App app = new App(subscriptionId, resourceGroup, appName)
            {
                DefaultHostName = postBody.DefaultHostName,
                Hostnames = postBody.HostNames != null ? postBody.HostNames.Select(p => p.Name) : new List<string>(),
                WebSpace = postBody.WebSpace,
                ScmSiteHostname = postBody.ScmSiteHostname,
                Stamp = await GetHostingEnvironment(postBody.Stamp.Subscription, postBody.Stamp.ResourceGroup, postBody.Stamp?.Name ?? string.Empty, postBody.Stamp, startTime, endTime, platformType),
                AppType = GetApplicationType(postBody.Kind),
                PlatformType = platformType,
                StackType =  StackType.All, //await this._siteService.GetApplicationStack(subscriptionId, resourceGroup, appName, (DataProviderContext)HttpContext.Items[HostConstants.DataProviderContextKey])
                // Not Fetching Stack Information anymore for kusto optimizations. Instead, detectors should look to leverage a gist created for the same.
                Tags = postBody.Tags
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

            if(Request.Headers.TryGetValue(HeaderConstants.SubscriptionLocationPlacementId, out StringValues subscriptionLocationPlacementId))
            {
                app.SubscriptionLocationPlacementId = subscriptionLocationPlacementId.FirstOrDefault();
            }

            return app;
        }

        private PlatformType GetPlatformType(DiagnosticSiteData postBody)
        {
            return postBody.IsXenon ? PlatformType.HyperV :
                !string.IsNullOrWhiteSpace(postBody.Kind) && postBody.Kind.ToLower().Contains("linux") ? PlatformType.Linux :
                PlatformType.Windows;
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
