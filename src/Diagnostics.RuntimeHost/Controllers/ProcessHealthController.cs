using System;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            List<Task> allChecks = new List<Task>();
            Task.Run(_sourceWatcherService.Watcher.WaitForFirstCompletion);
            Task.Run(_healthCheckService.RunHealthCheck);

            await Task.WhenAll(allChecks);

            return Ok("Server is up and running.");
        }
    }
}
