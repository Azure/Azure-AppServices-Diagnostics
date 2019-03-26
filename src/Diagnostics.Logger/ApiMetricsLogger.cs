// <copyright file="ApiMetricsLogger.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
namespace Diagnostics.Logger
{
    /// <summary>
    /// Api metrics logger.
    /// </summary>
    public class ApiMetricsLogger
    {
        private string requestId;
        private string address;
        private string verb;
        private DateTime startTime;
        private DateTime endTime;
        private long latencyInMs;
        private string subscriptionId;
        private string resourceGroupName;
        private string resourceName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiMetricsLogger"/> class.
        /// </summary>
        /// <param name="context">Http context.</param>
        public ApiMetricsLogger(HttpContext context)
        {
            StartMetricCapture(context);
        }

        /// <summary>
        /// Log runtime host API metrics.
        /// </summary>
        /// <param name="context">Http context.</param>
        public void LogRuntimeHostAPIMetrics(HttpContext context)
        {
            if (context == null)
            {
                return;
            }

            endTime = DateTime.UtcNow;
            latencyInMs = Convert.ToInt64((endTime - startTime).TotalMilliseconds);
            var headers = context.Request.Headers;
            List<object> headersContent = new List<object>();
            foreach (var header in headers)
            {
                if (header.Key.StartsWith("x-ms", StringComparison.OrdinalIgnoreCase))
                {
                    headersContent.Add(new
                    {
                        name = $"{header.Key}",
                        value = string.IsNullOrWhiteSpace(header.Value.FirstOrDefault()) ? string.Empty : "****"
                    });
                }
            }

            string contentString = JsonConvert.SerializeObject(new
            {
                requestHeaders = headersContent
            });
            DiagnosticsETWProvider.Instance.LogRuntimeHostAPISummary(
                requestId ?? string.Empty,
                subscriptionId ?? string.Empty,
                resourceGroupName ?? string.Empty,
                resourceName ?? string.Empty,
                address ?? string.Empty,
                verb ?? string.Empty,
                string.Empty,
                context.Response.StatusCode,
                latencyInMs,
                startTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                endTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                contentString);
        }

        /// <summary>
        /// Log compiler host API metrics.
        /// </summary>
        /// <param name="context">Http context.</param>
        public void LogCompilerHostAPIMetrics(HttpContext context)
        {
            if (context == null)
            {
                return;
            }

            endTime = DateTime.UtcNow;
            latencyInMs = Convert.ToInt64((endTime - startTime).TotalMilliseconds);

            DiagnosticsETWProvider.Instance.LogCompilerHostAPISummary(
                requestId ?? string.Empty,
                address ?? string.Empty,
                verb ?? string.Empty,
                context.Response.StatusCode,
                latencyInMs,
                startTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                endTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Log runtime host unhandled exception.
        /// </summary>
        /// <param name="context">Http contexnt.</param>
        /// <param name="ex">The exception.</param>
        public void LogRuntimeHostUnhandledException(HttpContext context, Exception ex)
        {
            if (context == null)
            {
                return;
            }

            DiagnosticsETWProvider.Instance.LogRuntimeHostUnhandledException(
                requestId ?? string.Empty,
                address ?? string.Empty,
                subscriptionId ?? string.Empty,
                resourceGroupName ?? string.Empty,
                resourceName ?? string.Empty,
                ex != null ? ex.GetType().ToString() : string.Empty,
                ex != null ? ex.ToString() : string.Empty);
        }

        /// <summary>
        /// Log compiler host unhandled exception.
        /// </summary>
        /// <param name="context">Http context.</param>
        /// <param name="ex">The exception.</param>
        public void LogCompilerHostUnhandledException(HttpContext context, Exception ex)
        {
            if (context == null)
            {
                return;
            }

            DiagnosticsETWProvider.Instance.LogCompilerHostUnhandledException(
                requestId ?? string.Empty,
                address ?? string.Empty,
                ex != null ? ex.GetType().ToString() : string.Empty,
                ex != null ? ex.ToString() : string.Empty);
        }

        private void StartMetricCapture(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues values) && values != default(StringValues) && values.Count > 0)
            {
                requestId = values[0];
            }

            startTime = DateTime.UtcNow;
            verb = context.Request.Method;
            address = context.Request.Path.ToString();

            ParseFromAddress(context);
        }

        private void ParseFromAddress(HttpContext httpContext)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(httpContext.Request.Path.ToString()))
                {
                    return;
                }

                Regex csmResourceRegEx = new Regex("(.*)subscriptions/(.*)/resourcegroups/(.*)/providers/(.*)/(.*)/(.*)/diagnostics/(.*)");

                string addressPath = httpContext.Request.Path.ToString().ToLower(CultureInfo.CurrentCulture);
                Match match = csmResourceRegEx.Match(addressPath);

                if (match.Success)
                {
                    subscriptionId = match.Groups[2].Value;
                    resourceGroupName = match.Groups[3].Value;
                    resourceName = match.Groups[6].Value;
                }
            }
            catch
            {
            }
        }
    }
}
