using Diagnostics.ModelsAndUtils;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Controllers
{
    public abstract class ControllerBase : Controller
    {
        public ControllerBase()
        {
        }
    }

    public abstract class SiteControllerBase : ControllerBase
    {
        protected IResourceService _resourceService;

        public SiteControllerBase(IResourceService resourceService)
        {
            _resourceService = resourceService;
        }

        protected bool VerifyQueryParams(string[] hostNames, string stampName, out string reason)
        {
            reason = string.Empty;
            if (hostNames == null || hostNames.Length <= 0)
            {
                reason = "Invalid or empty hostnames";
                return false;
            }

            if (string.IsNullOrWhiteSpace(stampName))
            {
                reason = "Invalid or empty stampName";
                return false;
            }

            return true;
        }
        
        protected OperationContext PrepareContext(SiteResource resource, DateTime startTime, DateTime endTime)
        {
            return new OperationContext(
                resource,
                DateTimeHelper.GetDateTimeInUtcFormat(startTime).ToString(HostConstants.KustoTimeFormat),
                DateTimeHelper.GetDateTimeInUtcFormat(endTime).ToString(HostConstants.KustoTimeFormat)
            );
        }
    }
}
