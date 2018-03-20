using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Diagnostics.RuntimeHost.Middleware
{
    public class DiagnosticsRequestMiddleware
    {
        private readonly RequestDelegate _next;

        public DiagnosticsRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            GenerateMissingRequestHeaders(ref httpContext);
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
            httpContext.Items.Add(HostConstants.ApiLoggerKey, logger);
        }

        private void EndRequest_Handle(HttpContext httpContext)
        {
            ApiMetricsLogger logger = (ApiMetricsLogger)httpContext.Items[HostConstants.ApiLoggerKey];
            if (logger == null)
            {
                logger = new ApiMetricsLogger(httpContext);
            }

            logger.LogRuntimeHostAPIMetrics(httpContext);
        }

        private void LogException(HttpContext context, Exception ex)
        {
            try
            {
                ApiMetricsLogger logger = (ApiMetricsLogger)context.Items[HostConstants.ApiLoggerKey];
                if (logger == null)
                {
                    logger = new ApiMetricsLogger(context);
                }

                logger.LogRuntimeHostUnhandledException(context, ex);
            }
            catch (Exception logEx)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostUnhandledException(
                    string.Empty,
                    "LogException_DiagnosticsRequestMiddleware",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    logEx.GetType().ToString(),
                    logEx.ToString());
            }
        }

        private void GenerateMissingRequestHeaders(ref HttpContext httpContext)
        {
            if (!httpContext.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues values)
                || values == default(StringValues) || !values.Any() || string.IsNullOrWhiteSpace(values.First()))
            {
                httpContext.Request.Headers[HeaderConstants.RequestIdHeaderName] = Guid.NewGuid().ToString();
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class DiagnosticsRequestMiddlewareExtensions
    {
        public static IApplicationBuilder UseDiagnosticsRequestMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DiagnosticsRequestMiddleware>();
        }
    }
}
