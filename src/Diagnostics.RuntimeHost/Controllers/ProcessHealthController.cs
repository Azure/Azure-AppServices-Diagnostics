using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Produces("application/json")]
    public class ProcessHealthController : Controller
    {
        private ISourceWatcherService _sourceWatcherService;

        public ProcessHealthController(ISourceWatcherService sourceWatcherService)
        {
            // This dependency is injected for source watcher service to start.
            _sourceWatcherService = sourceWatcherService;
        }

        [HttpGet(UriElements.HealthPing)]
        public IActionResult HealthPing()
        {
            return Ok("Server is up and running.");
        }
    }
}