using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Diagnostics.RuntimeHost.Utilities;
using System.Threading.Tasks;
using Diagnostics.Logger;
using System.Linq;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Authorize]
    [Produces("application/json")]
    public class DiagnosticResourceController: Controller
    {
        [HttpPost]
        [Route(UriElements.PassThroughAPIRoute)]
        public async Task<IActionResult> Invoke([FromBody] object postBody)
        {

            if (!Request.Headers.TryGetValue(HeaderConstants.ApiPathHeader, out var apiPaths) || !apiPaths.Any() || string.IsNullOrWhiteSpace(apiPaths.FirstOrDefault()))
            {
                return BadRequest($"Missing {HeaderConstants.ApiPathHeader} header");
            }

            if (!Request.Headers.TryGetValue(HeaderConstants.ApiVerbHeader, out var apiVerbs) || !apiVerbs.Any() || string.IsNullOrWhiteSpace(apiVerbs.FirstOrDefault()))
            {
                return BadRequest($"Missing {HeaderConstants.ApiVerbHeader} header");
            }

            string apiVerb = apiVerbs.First().ToLower();
            string apiPath = apiPaths.First().ToLower();

            switch (apiVerb)
            {
                case "post":
                      //return await base.PostRequest(apiPath, SerializeObect(postBody));
                case "get":
                    //return await base.GetRequest(apiPath);
                default:
                    return BadRequest($"Unsupported API Verb: {apiVerb.FirstOrDefault()}");                  
            }

        }
    }
}
