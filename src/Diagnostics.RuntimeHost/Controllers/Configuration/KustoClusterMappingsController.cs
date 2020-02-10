// <copyright file="KustoClusterMappingsController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diagnostics.RuntimeHost.Controllers.Configuration
{
    [Authorize]
    [Produces("application/json")]
    [Route(UriElements.ArmResource + "/" + UriElements.KustoClusterMappings)]
    public class KustoClusterMappingsController : Controller
    {
        public KustoClusterMappingsController()
        {

        }

        [HttpPost]
        public IActionResult AddOrUpdateMapping(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, [FromBody]TablePostBody kustoMappings)
        {
            throw new NotImplementedException();
        }

        [HttpGet(UriElements.UniqueResourceId)]
        public Task<IActionResult> GetMapping(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, string id)
        {
            throw new NotImplementedException();
        }

        [HttpDelete(UriElements.UniqueResourceId)]
        public Task<IActionResult> DeleteMapping(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, string id)
        {
            throw new NotImplementedException();
        }
    }
}
