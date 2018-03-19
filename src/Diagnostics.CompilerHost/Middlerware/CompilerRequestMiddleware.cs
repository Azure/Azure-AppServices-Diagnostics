using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Diagnostics.CompilerHost
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
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
            ApiMetricsLogger logger = (ApiMetricsLogger)httpContext.Items[_apiLoggerKey];
            if (logger == null)
            {
                logger = new ApiMetricsLogger(httpContext);
            }

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
                DiagnosticsETWProvider.Instance.LogCompilerHostUnhandledException(
                    string.Empty,
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
