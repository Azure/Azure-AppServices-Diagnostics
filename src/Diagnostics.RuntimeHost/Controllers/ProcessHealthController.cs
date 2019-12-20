using System;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Produces("application/json")]
    public class ProcessHealthController : Controller
    {
        private ISourceWatcherService _sourceWatcherService;
        private ICompilerHostClient _compilerHostClient;
        private IHealthCheckService _healthCheckService;

        public ProcessHealthController(ISourceWatcherService sourceWatcherService, ICompilerHostClient compilerHostClient, IHealthCheckService healthCheckService)
        {
            // These dependencies are injected for the services to start.
            _sourceWatcherService = sourceWatcherService;
            _compilerHostClient = compilerHostClient;
            _healthCheckService = healthCheckService;
        }

        [HttpGet(UriElements.HealthPing)]
        public async Task<IActionResult> HealthPing()
        {
            await _sourceWatcherService.Watcher.WaitForFirstCompletion();
            await _healthCheckService.RunHealthCheck();

            return Ok("Server is up and running.");
        }
    }
}
