// <copyright file="CompilerHostController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Diagnostics.CompilerHost.Controllers
{
    /// <summary>
    /// Compiler host controller.
    /// </summary>
    [Route("api/[controller]")]
    public class CompilerHostController : Controller
    {
        /// <summary>
        /// Health ping.
        /// </summary>
        /// <returns>Action result.</returns>
        [HttpGet("healthping")]
        public IActionResult HealthPing()
        {
            return Ok("Server is up and running.");
        }

        /// <summary>
        /// Action for handling post request.
        /// </summary>
        /// <param name="jsonBody">Json request body.</param>
        /// <returns>Action result.</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody]JToken jsonBody)
        {
            if (jsonBody == null)
            {
                return BadRequest("Missing body");
            }

            // Get script and reference
            var script = jsonBody.Value<string>("script");

            if (string.IsNullOrWhiteSpace(script))
            {
                return BadRequest("Missing script from body");
            }

            var references = TryExtract(jsonBody, "reference") ?? new Dictionary<string, string>();

            if (!Enum.TryParse(jsonBody.Value<string>("entityType"), true, out EntityType entityType))
            {
                entityType = EntityType.Signal;
            }

            var metaData = new EntityMetadata(script, entityType);
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
        private static Dictionary<string, string> TryExtract(JToken jsonBody, string key)
        {
            try
            {
                return jsonBody[key].ToObject<Dictionary<string, string>>();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
