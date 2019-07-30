using System;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Produces("application/json")]
    public class ProcessHealthController : Controller
    {
        [HttpGet(UriElements.HealthPing)]
        public IActionResult HealthPing()
        {
            return Ok("Server is up and running.");
        }
    }
}
