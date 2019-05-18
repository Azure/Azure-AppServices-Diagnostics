using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Produces("application/json")]
    public class ProcessHealthController : Controller
    {
        private ISourceWatcherService _sourceWatcherService;
        private ICompilerHostClient _compilerHostClient;

        public ProcessHealthController(IServiceProvider services)
        {
            // These dependencies are injected for the services to start.
            _sourceWatcherService = (ISourceWatcherService)services.GetService(typeof(ISourceWatcherService));
            _compilerHostClient = (ICompilerHostClient)services.GetService(typeof(ICompilerHostClient));
        }

        [HttpGet(UriElements.HealthPing)]
        public IActionResult HealthPing()
        {
            return Ok("Server is up and running.");
        }
    }
}