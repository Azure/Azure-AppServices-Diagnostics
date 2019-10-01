using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Diagnostics.RuntimeHost.Controllers
{
    /// <summary>
    /// This API is used to get resource informatio directly from the observer data provider.
    /// </summary>
    [Produces("application/json")]
    [Route(UriElements.Observer)]
    public class ObserverController : Controller
    {
        [HttpGet(UriElements.ObserverGetSites)]
        public async Task<IActionResult> GetSiteDetails(string siteName)
        {
            return await GetResultAsync($"/sites/{siteName}/adminsites");
        }

        [HttpGet(UriElements.ObserverGetSiteWithStamp)]
        public async Task<IActionResult> GetSiteWithStampDetails(string stampName, string siteName)
        {
            return await GetResultAsync($"/stamps/{stampName}/sites/{siteName}/adminsites");
        }

        [HttpGet(UriElements.ObserverGetHostingEnvironment)]
        public async Task<IActionResult> GetHostingEnvironmentDetails(string hostingEnvironmentName)
        {
            return await GetResultAsync($"/hostingEnvironments/{hostingEnvironmentName}");
        }

        private async Task<IActionResult> GetResultAsync(string path)
        {
            ActionResult apiResponse;
            HttpStatusCode observerStatusCode = default(HttpStatusCode);
            var dataProviderContext = (DataProviderContext)HttpContext.Items[HostConstants.DataProviderContextKey];
            var dataProviders = new DataProviders.DataProviders(dataProviderContext);

            var uriBuilder = new UriBuilder(dataProviderContext.Configuration.SupportObserverConfiguration.Endpoint)
            {
                Path = path
            };

            object result = null;
            try
            {
                result = await dataProviders.Observer.GetResource(uriBuilder.ToString());
                observerStatusCode = HttpStatusCode.OK;
            }
            catch (HttpRequestException observerHttpException)
            {
                observerStatusCode = (HttpStatusCode)observerHttpException.Data["StatusCode"];
                result = (string)observerHttpException.Data["ResponseContent"];
            }
            finally
            {
                apiResponse = StatusCode((int)observerStatusCode, result);
            }

            return apiResponse;
        }
    }
}
