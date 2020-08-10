// <copyright file="CompilerRequestMiddleware.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Diagnostics.CompilerHost.Middleware
{
    /// <summary>
    /// The compiler request middleware.
    /// </summary>
    public class CompilerRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _apiLoggerKey = "CompilerHost_ApiLogger";

        public CompilerRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            BeginRequest_Handle(httpContext);

            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                if (!httpContext.Response.HasStarted)
                {
                    httpContext.Response.Clear();
                    httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

                LogException(httpContext, ex);
            }
            finally
            {
                EndRequest_Handle(httpContext);
            }

            return;
        }

        private void BeginRequest_Handle(HttpContext httpContext)
        {
            var logger = new ApiMetricsLogger(httpContext);
            httpContext.Items.Add(_apiLoggerKey, logger);
        }

        private void EndRequest_Handle(HttpContext httpContext)
        {
            var logger = (ApiMetricsLogger)httpContext.Items[_apiLoggerKey] ?? new ApiMetricsLogger(httpContext);

            logger.LogCompilerHostAPIMetrics(httpContext);
        }

        private void LogException(HttpContext context, Exception ex)
        {
            try
            {
                ApiMetricsLogger logger = (ApiMetricsLogger)context.Items[_apiLoggerKey];
                if (logger == null)
                {
                    logger = new ApiMetricsLogger(context);
                }

                logger.LogCompilerHostUnhandledException(context, ex);
            }
            catch (Exception logEx)
            {
                string requestId = string.Empty;
                if (context.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues values) && values != default(StringValues) && values.Any())
                {
                    requestId = values.First().Split(new char[] { ',' })[0];
                }

                DiagnosticsETWProvider.Instance.LogCompilerHostUnhandledException(
                    requestId,
                    "LogException_CompilerRequestMiddleware",
                    logEx.GetType().ToString(),
                    logEx.ToString());
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class CompilerRequestMiddlewareExtensions
    {
        public static IApplicationBuilder UseCompilerRequestMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CompilerRequestMiddleware>();
        }
    }
}
