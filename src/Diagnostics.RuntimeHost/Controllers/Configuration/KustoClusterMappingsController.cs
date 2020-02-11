// <copyright file="KustoClusterMappingsController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Controllers.Configuration
{
    [Authorize]
    [Produces("application/json")]
    [Route(UriElements.ArmResource + "/" + UriElements.KustoClusterMappings)]
    public class KustoClusterMappingsController : Controller
    {
        private ISourceWatcherService _sourceWatcherService;

        public KustoClusterMappingsController(ISourceWatcherService sourceWatcherService)
        {
            _sourceWatcherService = sourceWatcherService;
        }

        [HttpPost]
        public async Task<IActionResult> AddOrUpdateMapping(string provider, [FromBody]Table kustoMappings)
        {
            var gitHubPackage = new GithubPackage(GetGitHubId(provider), "kustoClusterMappings", "json", JsonConvert.SerializeObject(kustoMappings));

            try
            {
                await _sourceWatcherService.Watcher.CreateOrUpdatePackage(gitHubPackage);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public Task<IActionResult> GetMappings(string provider)
        {
            throw new NotImplementedException();
        }

        [HttpDelete(UriElements.UniqueResourceId)]
        public Task<IActionResult> DeleteMapping(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, string id)
        {
            throw new NotImplementedException();
        }

        private string GetGitHubId(string provider)
        {
            return $"{provider.Replace(".", string.Empty)}Configuration";
        }
    }
}
