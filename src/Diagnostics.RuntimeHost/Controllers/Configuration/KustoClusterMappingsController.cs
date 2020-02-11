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
using Diagnostics.RuntimeHost.Services.CacheService;
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
        private IKustoMappingsCacheService _kustoMappingsCache;

        public KustoClusterMappingsController(ISourceWatcherService sourceWatcherService, IKustoMappingsCacheService kustoMappingsCache)
        {
            _sourceWatcherService = sourceWatcherService;
            _kustoMappingsCache = kustoMappingsCache;
        }

        [HttpPost]
        public async Task<IActionResult> AddOrUpdateMapping(string provider, [FromBody]List<Dictionary<string, string>> kustoMappings)
        {
            var cacheId = GetGitHubId(provider);
            await _sourceWatcherService.Watcher.WaitForFirstCompletion();

            //TODO the equals condition always fail due to the strings in the data structures are never compared
            if (_kustoMappingsCache.TryGetValue(cacheId, out List<Dictionary<string, string>> cacheValue) && cacheValue.Equals(kustoMappings))
            {
                return Ok();
            }

            var gitHubPackage = new GithubPackage(cacheId, "kustoClusterMappings", "json", JsonConvert.SerializeObject(kustoMappings));

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
        public async Task<IActionResult> GetMappings(string provider)
        {
            var cacheId = GetGitHubId(provider);
            await _sourceWatcherService.Watcher.WaitForFirstCompletion();
            
            if (_kustoMappingsCache.TryGetValue(cacheId, out List<Dictionary<string, string>> value))
            {
                return Ok(value);
            }

            return NotFound();
        }

        [HttpDelete]
        public Task<IActionResult> DeleteMapping(string provider)
        {
            throw new NotImplementedException();
        }

        private string GetGitHubId(string provider)
        {
            return $"{provider.Replace(".", string.Empty)}Configuration";
        }
    }
}
