using System;
using Microsoft.AspNetCore.Rewrite;
using Diagnostics.Logger;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Diagnostics.RuntimeHost.Utilities;

namespace Diagnostics.RuntimeHost.Utilities
{
    public class RewriteDiagnosticResource : IRule
    {
        public int StatusCode { get; } = (int)HttpStatusCode.BadRequest;

        public void ApplyRule(RewriteContext context)
        {
            var request = context.HttpContext.Request;
            // Partner teams using Diagnose and Solve, make the request to api/invoke from their RP.
            if (!request.Path.Value.Equals(UriElements.PassThroughAPIRoute, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = RuleResult.ContinueRules;
                return;
            } else
            {
                // Check for required headers and send 400 if not present.
                if (!request.Headers.TryGetValue(HeaderConstants.ApiPathHeader, out var apiPaths) || !apiPaths.Any() || string.IsNullOrWhiteSpace(apiPaths.FirstOrDefault()))
                {
                    var response = context.HttpContext.Response;
                    response.StatusCode = this.StatusCode;
                    response.ContentType = "text/plain";
                    response.WriteAsync($"Missing {HeaderConstants.ApiPathHeader} header").Wait();
                    context.Result = RuleResult.EndResponse; // Send response and do not continue the request.
                    return;
                }

                if (!request.Headers.TryGetValue(HeaderConstants.ApiVerbHeader, out var apiVerbs) || !apiVerbs.Any() || string.IsNullOrWhiteSpace(apiVerbs.FirstOrDefault()))
                {
                    var response = context.HttpContext.Response;
                    response.StatusCode = this.StatusCode;
                    response.ContentType = "text/plain";
                    response.WriteAsync($"Missing {HeaderConstants.ApiVerbHeader} header").Wait();
                    context.Result = RuleResult.EndResponse; // Send response and do not continue the request.
                    return;
                }

                string apiVerb = apiVerbs.First().ToLower();
                string apiPath = apiPaths.First().ToLower();
                request.Path = apiPath;
                request.Method = apiVerb.ToUpper();
                context.Result = RuleResult.SkipRemainingRules; // Continue request to next middleware.
            }
        }
    }
}
