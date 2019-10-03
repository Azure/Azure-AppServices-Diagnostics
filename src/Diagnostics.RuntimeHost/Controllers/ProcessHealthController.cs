using System;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Produces("application/json")]
    public class ProcessHealthController : Controller
    {
        private ISourceWatcherService _sourceWatcherService;
        private ICompilerHostClient _compilerHostClient;

        public ProcessHealthController(ISourceWatcherService sourceWatcherService, ICompilerHostClient compilerHostClient)
        {
            // These dependencies are injected for the services to start.
            _sourceWatcherService = sourceWatcherService;
            _compilerHostClient = compilerHostClient;
        }

        [HttpGet(UriElements.HealthPing)]
        public async Task<IActionResult> HealthPing()
        {
            if (_sourceWatcherService.Watcher is GitHubWatcher githubWatcher)
            {
                await githubWatcher.WaitForFirstCompletion();
            }
            return Ok("Server is up and running.");
        }
    }
}
