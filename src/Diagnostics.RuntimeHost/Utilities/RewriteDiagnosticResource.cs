﻿using System;
using Microsoft.AspNetCore.Rewrite;
using Diagnostics.Logger;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Diagnostics.RuntimeHost.Utilities
{
    public class RewriteDiagnosticResource : IRule
    {
        public int StatusCode { get; } = (int)HttpStatusCode.BadRequest;

        public void ApplyRule(RewriteContext context)
        {
            var request = context.HttpContext.Request;
            // Partner teams using Diagnose and Solve, make the request to api/invoke from their RP.
            if (!request.Path.Value.Equals(UriElements.PassThroughAPIRoute, StringComparison.OrdinalIgnoreCase) && !request.Path.Value.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = RuleResult.ContinueRules;
                return;
            }
            else
            {
                // Check for required headers and send 400 if not present.
                if (!request.Headers.TryGetValue(HeaderConstants.ApiPathHeader, out var apiPaths) || !apiPaths.Any() || string.IsNullOrWhiteSpace(apiPaths.FirstOrDefault()))
                {
                    var response = context.HttpContext.Response;
                    response.StatusCode = this.StatusCode;
                    response.ContentType = "text/plain";
                    byte[] errorMessage = Encoding.ASCII.GetBytes($"Missing {HeaderConstants.ApiPathHeader} header");
                    response.Body.WriteAsync(errorMessage, 0, errorMessage.Length);               
                    context.Result = RuleResult.EndResponse; // Send response and do not continue the request.
                    return;
                }

                if (!request.Headers.TryGetValue(HeaderConstants.ApiVerbHeader, out var apiVerbs) || !apiVerbs.Any() || string.IsNullOrWhiteSpace(apiVerbs.FirstOrDefault()))
                {
                    var response = context.HttpContext.Response;
                    response.StatusCode = this.StatusCode;
                    response.ContentType = "text/plain";
                    byte[] errorMessage = Encoding.ASCII.GetBytes($"Missing {HeaderConstants.ApiVerbHeader} header");
                    response.Body.WriteAsync(errorMessage, 0, errorMessage.Length);
                    context.Result = RuleResult.EndResponse; // Send response and do not continue the request.
                    return;
                }

                const string contentTypeHeader = "Content-Type";
                const string contentTypeHeaderValue = "application/json";

                if (!request.Headers.TryGetValue(contentTypeHeader, out StringValues requestedContentTypes))
                {
                    request.Headers.Add(contentTypeHeader, new StringValues(contentTypeHeaderValue));
                }
                else if (!requestedContentTypes.Any(hv => hv.Equals("application/json")))
                {
                    request.Headers.Append(contentTypeHeader, new StringValues(contentTypeHeaderValue));
                }

                request.Method = apiVerbs.First().ToLower();
                var rewriteOptions = new RewriteOptions().AddRewrite(UriElements.PassThroughAPIRoute.Substring(1) + "|^$", apiPaths.First().ToLower(), true);
                var rewriteRule = rewriteOptions.Rules.FirstOrDefault();
                rewriteRule.ApplyRule(context);         
            }
        }
    }
}
