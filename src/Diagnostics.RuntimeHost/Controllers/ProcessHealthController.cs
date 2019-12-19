using System;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Produces("application/json")]
    public class ProcessHealthController : Controller
    {
        private ISourceWatcherService _sourceWatcherService;
        private ICompilerHostClient _compilerHostClient;
        private IConfiguration _configuration;

        public ProcessHealthController(ISourceWatcherService sourceWatcherService, ICompilerHostClient compilerHostClient, IConfiguration Configuration)
        {
            // These dependencies are injected for the services to start.
            _sourceWatcherService = sourceWatcherService;
            _compilerHostClient = compilerHostClient;
            _configuration = Configuration;
        }

        public static bool PingExternal(string pingUrl)
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead(pingUrl))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet(UriElements.HealthPing)]
        public async Task<IActionResult> HealthPing()
        {
            await _sourceWatcherService.Watcher.WaitForFirstCompletion();

            bool isCheckEnabled = Convert.ToBoolean(_configuration["OutboundConnectivity:IsCheckEnabled"]);
            if (isCheckEnabled)
            {
                bool externalPing = PingExternal(_configuration["OutboundConnectivity:PingUrl"]);
                if (!externalPing)
                {
                    return StatusCode(500);
                }
            }
            return Ok("Server is up and running.");
        }
    }
}
