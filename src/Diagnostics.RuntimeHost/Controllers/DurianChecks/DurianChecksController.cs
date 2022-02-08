using System;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Diagnostics.RuntimeHost.Controllers
{
    // Durian API to check user group and access
    [Authorize]
    [Produces("application/json")]
    [Route("/" + UriElements.Durian)]
    public class DurianChecksController : Controller
    {
        protected IUserAuthHandler _userAuthHandler;

        public DurianChecksController(IServiceProvider services)
        {
            _userAuthHandler = (IUserAuthHandler)services.GetService(typeof(IUserAuthHandler));
        }

        /// <summary>
        /// Check User access
        /// </summary>
        /// <returns>Task for checking User access.</returns>
        [HttpGet(UriElements.CheckUserAccess)]
        public async Task<IActionResult> CheckUserAccess()
        {
            return Ok();
        }
    }
}
