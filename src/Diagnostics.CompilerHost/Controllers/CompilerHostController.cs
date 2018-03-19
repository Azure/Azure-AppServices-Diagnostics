using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Diagnostics.CompilerHost.Controllers
{
    [Route("api/[controller]")]
    public class CompilerHostController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]JToken jsonBody)
        {
            if (jsonBody == null)
            {
                return BadRequest("Missing body");
            }

            string script = jsonBody.Value<string>("script");

            if (string.IsNullOrWhiteSpace(script))
            {
                return BadRequest("Missing script from body");
            }

            EntityMetadata metaData = new EntityMetadata(script);
            CompilerResponse compilerResponse = new CompilerResponse();
            using (var invoker = new EntityInvoker(metaData, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                compilerResponse.CompilationOutput = invoker.CompilationOutput;
                compilerResponse.CompilationSucceeded = invoker.IsCompilationSuccessful;

                if (compilerResponse.CompilationSucceeded)
                {
                    Tuple<string, string> asmBytes = await invoker.GetAssemblyBytesAsync();
                    compilerResponse.AssemblyBytes = asmBytes.Item1;
                    compilerResponse.PdbBytes = asmBytes.Item2;
                }
            }

            return Ok(compilerResponse);
        }
    }
}
