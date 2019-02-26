using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diagnostics.CompilerHost.Controllers
{
    [Route("api/[controller]")]
    public class CompilerHostController : Controller
    {
        [HttpGet("healthping")]
        public IActionResult HealthPing()
        {
            return Ok("Server is up and running.");
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]JToken jsonBody)
        {
            if (jsonBody == null)
            {
                return BadRequest("Missing body");
            }

            // Get script and reference
            var script = jsonBody.Value<string>("script");

            var references = new Dictionary<string, string>();
            if (jsonBody["reference"] != null)
            {
                references = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonBody["reference"].ToString());
            }

            if (string.IsNullOrWhiteSpace(script))
            {
                return BadRequest("Missing script from body");
            }

            var metaData = new EntityMetadata(script);
            var compilerResponse = new CompilerResponse();
            using (var invoker = new EntityInvoker(metaData, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports(), references.ToImmutableDictionary()))
            {
                await invoker.InitializeEntryPointAsync();
                compilerResponse.CompilationTraces = invoker.CompilationOutput;
                compilerResponse.CompilationSucceeded = invoker.IsCompilationSuccessful;
                compilerResponse.References = invoker.References;

                if (compilerResponse.CompilationSucceeded)
                {
                    var asmBytes = await invoker.GetAssemblyBytesAsync();
                    compilerResponse.AssemblyBytes = asmBytes.Item1;
                    compilerResponse.PdbBytes = asmBytes.Item2;
                }
            }

            return Ok(compilerResponse);
        }
    }
}
