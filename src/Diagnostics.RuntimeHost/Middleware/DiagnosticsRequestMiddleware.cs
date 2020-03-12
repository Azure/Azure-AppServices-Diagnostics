using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using static Diagnostics.Logger.HeaderConstants;
using Diagnostics.RuntimeHost.Models.Exceptions;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Diagnostics.RuntimeHost.Middleware
{
    public class DiagnosticsRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DiagnosticsRequestMiddleware> _logger;

        public DiagnosticsRequestMiddleware(RequestDelegate next, ILogger<DiagnosticsRequestMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            Exception exception = null;
            int statusCode = 0;
            string responseMessage = null;
            GenerateMissingRequestHeaders(httpContext);
            BeginRequestHandle(httpContext);

            try
            {
                await _next(httpContext);
            }
            catch (TimeoutException ex)
            {
                exception = ex;
                statusCode = (int)HttpStatusCode.RequestTimeout;
            }
            catch (ASETenantListEmptyException ex)
            {
                exception = ex;
                statusCode = 424;
                responseMessage = ErrorMessages.ASETenantListEmptyErrorMessage;
            }
            catch (Exception ex)
            {
                exception = ex;
                statusCode = (int)HttpStatusCode.InternalServerError;
                throw;
            }
            finally
            {
                if (exception != null && !httpContext.Response.HasStarted)
                {
                    httpContext.Response.Clear();
                    httpContext.Response.StatusCode = statusCode;
                    if (responseMessage != null)
                    {
                        await httpContext.Response.WriteAsync(responseMessage).ConfigureAwait(false);
                    }
                    LogException(httpContext, exception);
                }

                EndRequestHandle(httpContext);
            }

            return;
        }

        private void BeginRequestHandle(HttpContext httpContext)
        {
            var logger = new ApiMetricsLogger(httpContext);
            var cTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(HostConstants.TimeoutInMilliSeconds));

            httpContext.RequestAborted = cTokenSource.Token;
            httpContext.Request.Headers.TryGetValue(RequestIdHeaderName, out StringValues values);
            var queryStringValues = httpContext.Request.Query?.ToDictionary(query => query.Key.ToLower(), query => query.Value.FirstOrDefault())
                ?? new Dictionary<string, string>();

            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(
                queryStringValues.GetValueOrDefault("starttime", null),
                queryStringValues.GetValueOrDefault("endtime", null),
                queryStringValues.GetValueOrDefault("timegrain", null),
                out DateTime startTimeUtc,
                out DateTime endTimeUtc,
                out TimeSpan timeGrainTimeSpan,
                out string errorMessage);

            var dataSourcesConfigurationService = ((ServiceProvider)httpContext.RequestServices).GetService<IDataSourcesConfigurationService>();
            var wawsObserverTokenService = ((ServiceProvider)httpContext.RequestServices).GetService<IWawsObserverTokenService>();
            var supportBayApiObserverTokenService = ((ServiceProvider)httpContext.RequestServices).GetService<ISupportBayApiObserverTokenService>();
            var kustoHeartBeatService = ((ServiceProvider)httpContext.RequestServices).GetService<IKustoHeartBeatService>();

            httpContext.Items.Add(HostConstants.ApiLoggerKey, logger);
            var clientObjId = string.Empty;
            var isInternalClient = false;

            // For requests coming Applens, populate client object id with Applens App Id.
            if (httpContext.Request.Headers.TryGetValue(InternalClientHeader, out var internalClientHeader) && internalClientHeader.Any())
            {
                bool.TryParse(internalClientHeader.First(), out isInternalClient);
            }

            if (isInternalClient)
            {
                clientObjId = dataSourcesConfigurationService.Config.ChangeAnalysisDataProviderConfiguration.ClientId;
            }
            else if (httpContext.Request.Headers.ContainsKey(ClientObjectIdHeader))
            {
                clientObjId = httpContext.Request.Headers[ClientObjectIdHeader];
            }

            string clientPrincipalName = string.Empty;
            if (httpContext.Request.Headers.ContainsKey(ClientPrincipalNameHeader))
            {
                clientPrincipalName = httpContext.Request.Headers[ClientPrincipalNameHeader];
            }

            string geomasterHostName = string.Empty;
            string geomasterName = string.Empty;

            if (httpContext.Request.Headers.ContainsKey(GeomasterHostNameHeader))
            {
                geomasterHostName = httpContext.Request.Headers[GeomasterHostNameHeader];
            }

            if (httpContext.Request.Headers.ContainsKey(GeomasterNameHeader))
            {
                geomasterName = httpContext.Request.Headers[GeomasterNameHeader];
            }

            httpContext.Items.Add(HostConstants.DataProviderContextKey, new DataProviderContext(dataSourcesConfigurationService.Config, values.FirstOrDefault() ?? string.Empty, cTokenSource.Token, startTimeUtc, endTimeUtc, wawsObserverTokenService, supportBayApiObserverTokenService, clientObjId, clientPrincipalName, kustoHeartBeatService, geomasterHostName, geomasterName, null, httpContext.Request.Headers));
        }

        private void EndRequestHandle(HttpContext httpContext)
        {
            var logger = (ApiMetricsLogger)httpContext.Items[HostConstants.ApiLoggerKey] ?? new ApiMetricsLogger(httpContext);

            logger.LogRuntimeHostAPIMetrics(httpContext);
        }

        private void LogException(HttpContext context, Exception ex)
        {
            _logger.LogError(ex, "Failed to process request for {request} Exception Type: {type} Exception Message: {message}", context.Request.Path.Value, ex.GetType().ToString(), ex.Message);

            try
            {
                var logger = (ApiMetricsLogger)context.Items[HostConstants.ApiLoggerKey] ?? new ApiMetricsLogger(context);

                logger.LogRuntimeHostUnhandledException(context, ex);
            }
            catch (Exception logEx)
            {
                string requestId = string.Empty;
                if (context.Request.Headers.TryGetValue(RequestIdHeaderName, out StringValues values) && values != default(StringValues) && values.Any())
                {
                    requestId = values.First().ToString();
                }

                DiagnosticsETWProvider.Instance.LogRuntimeHostUnhandledException(
                    requestId,
                    "LogException_DiagnosticsRequestMiddleware",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    logEx.GetType().ToString(),
                    logEx.ToString());
            }
        }

        private void GenerateMissingRequestHeaders(HttpContext httpContext)
        {
            if (!httpContext.Request.Headers.TryGetValue(RequestIdHeaderName, out StringValues values)
                || values == default(StringValues) || !values.Any() || string.IsNullOrWhiteSpace(values.First()))
            {
                httpContext.Request.Headers[RequestIdHeaderName] = Guid.NewGuid().ToString();
            }
        }
    }
}
