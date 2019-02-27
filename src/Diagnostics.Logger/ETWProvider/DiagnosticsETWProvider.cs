// <copyright file="DiagnosticsETWProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System.Diagnostics.Tracing;

namespace Diagnostics.Logger
{
    /// <summary>
    /// Diagnostics ETW provider.
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-AppService-Diagnostics")]
    public sealed class DiagnosticsETWProvider : DiagnosticsEventSourceBase
    {
        /// <summary>
        /// ETW provider instance.
        /// </summary>
        public static readonly DiagnosticsETWProvider Instance = new DiagnosticsETWProvider();

        #region Compile Host Events (ID Range : 1000 - 1999)

        /// <summary>
        /// Log compiler host message.
        /// </summary>
        /// <param name="message">The message.</param>
        [Event(1000, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostMessage)]
        public void LogCompilerHostMessage(string message)
        {
            WriteDiagnosticsEvent(1000, message);
        }

        /// <summary>
        /// Log compiler host unhandled exception.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="exceptionDetails">Exception details.</param>
        [Event(1001, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostUnhandledException)]
        public void LogCompilerHostUnhandledException(string requestId, string source, string exceptionType, string exceptionDetails)
        {
            WriteDiagnosticsEvent(
                1001,
                requestId,
                source,
                exceptionType,
                exceptionDetails);
        }

        /// <summary>
        /// Log compiler host API summary.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="address">The address.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="latencyInMilliseconds">The latency.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        [Event(1002, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostAPISummary)]
        public void LogCompilerHostAPISummary(string requestId, string address, string verb, int statusCode, long latencyInMilliseconds, string startTime, string endTime)
        {
            WriteDiagnosticsEvent(
                1002,
                requestId,
                address,
                verb,
                statusCode,
                latencyInMilliseconds,
                startTime,
                endTime);
        }

        #endregion

        #region Runtime Host Events (ID Range : 2000 - 2499)

        /// <summary>
        /// Log runtime host message.
        /// </summary>
        /// <param name="message">The message.</param>
        [Event(2000, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostMessage)]
        public void LogRuntimeHostMessage(string message)
        {
            WriteDiagnosticsEvent(2000, message);
        }

        /// <summary>
        /// Log runtime host unhandled exception.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="resourceGroup">The resource group.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="exceptionDetails">Exception details.</param>
        [Event(2001, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostUnhandledException)]
        public void LogRuntimeHostUnhandledException(string requestId, string source, string subscriptionId, string resourceGroup, string resource, string exceptionType, string exceptionDetails)
        {
            WriteDiagnosticsEvent(
                2001,
                requestId,
                source,
                subscriptionId,
                resourceGroup,
                resource,
                exceptionType,
                exceptionDetails);
        }

        /// <summary>
        /// Log runtime host API summary.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="resourceGroup">The resource group.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="address">The address.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="operationName">Operation time.</param>
        /// <param name="statusCode">Status code.</param>
        /// <param name="latencyInMilliseconds">The latency.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        [Event(2002, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostAPISummary)]
        public void LogRuntimeHostAPISummary(string requestId, string subscriptionId, string resourceGroup, string resource, string address, string verb, string operationName, int statusCode, long latencyInMilliseconds, string startTime, string endTime)
        {
            WriteDiagnosticsEvent(
                2002,
                requestId,
                subscriptionId,
                resourceGroup,
                resource,
                address,
                verb,
                operationName,
                statusCode,
                latencyInMilliseconds,
                startTime,
                endTime);
        }

        /// <summary>
        /// Log retry attempt message.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        [Event(2003, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRetryAttemptMessage)]
        public void LogRetryAttemptMessage(string requestId, string source, string message)
        {
            WriteDiagnosticsEvent(
                2003,
                requestId,
                source,
                message);
        }

        /// <summary>
        /// Log retry attempt summary.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        /// <param name="latencyInMilliseconds">The latency.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="exceptionDetails">Exception details.</param>
        [Event(2004, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRetryAttemptSummary)]
        public void LogRetryAttemptSummary(string requestId, string source, string message, long latencyInMilliseconds, string startTime, string endTime, string exceptionType, string exceptionDetails)
        {
            WriteDiagnosticsEvent(
                2004,
                requestId,
                source,
                message,
                latencyInMilliseconds,
                startTime,
                endTime,
                exceptionType,
                exceptionDetails);
        }

        /// <summary>
        /// Log runtime host insight correlation.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="resourceGroup">The resource group.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="correlationId">The correlation id.</param>
        /// <param name="message">The message.</param>
        [Event(2005, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostInsightsCorrelation, Version = 2)]
        public void LogRuntimeHostInsightCorrelation(string requestId, string source, string subscriptionId, string resourceGroup, string resource, string correlationId, string message)
        {
            WriteDiagnosticsEvent(
                2005,
                requestId,
                source,
                subscriptionId,
                resourceGroup,
                resource,
                correlationId,
                message);
        }

        /// <summary>
        /// Log runtime host handled exception.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroup">Resource group.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="exceptionDetails">Exception details.</param>
        [Event(2006, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostHandledException)]
        public void LogRuntimeHostHandledException(string requestId, string source, string subscriptionId, string resourceGroup, string resource, string exceptionType, string exceptionDetails)
        {
            WriteDiagnosticsEvent(
                2006,
                requestId,
                source,
                subscriptionId,
                resourceGroup,
                resource,
                exceptionType,
                exceptionDetails);
        }

        /// <summary>
        /// Log full ASC insight.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        /// <param name="details">The details.</param>
        [Event(2008, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostSupportTopicAscInsight)]
        public void LogFullAscInsight(string requestId, string source, string message, string details)
        {
            WriteDiagnosticsEvent(
                2008,
                requestId,
                source,
                message,
                details);
        }

        #endregion

        #region SourceWatcher Events (ID Range : 2500 - 2599)

        /// <summary>
        /// Log source watch message.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        [Event(2500, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogSourceWatcherMessage)]
        public void LogSourceWatcherMessage(string source, string message)
        {
            WriteDiagnosticsEvent(2500, source, message);
        }

        /// <summary>
        /// Log source watcher warning.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        [Event(2501, Level = EventLevel.Warning, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogSourceWatcherWarning)]
        public void LogSourceWatcherWarning(string source, string message)
        {
            WriteDiagnosticsEvent(2501, source, message);
        }

        /// <summary>
        /// Log source watcher exception.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="exceptionDetails">Exception details.</param>
        [Event(2502, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogSourceWatcherException)]
        public void LogSourceWatcherException(string source, string message, string exceptionType, string exceptionDetails)
        {
            WriteDiagnosticsEvent(2502, source, message, exceptionType, exceptionDetails);
        }

        #endregion

        #region Compiler Host Client Events (ID Range: 2600 - 2699)

        /// <summary>
        /// Log compiler host client message.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        [Event(2600, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostClientMessage)]
        public void LogCompilerHostClientMessage(string requestId, string source, string message)
        {
            WriteDiagnosticsEvent(2600, requestId, source, message);
        }

        /// <summary>
        /// Log compiler host client exception.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="exceptionDetails">Exception details.</param>
        [Event(2601, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostClientException)]
        public void LogCompilerHostClientException(string requestId, string source, string message, string exceptionType, string exceptionDetails)
        {
            WriteDiagnosticsEvent(2601, requestId, source, message, exceptionType, exceptionDetails);
        }

        /// <summary>
        /// Log compiler host client warning.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="exceptionDetails">Exception details.</param>
        [Event(2602, Level = EventLevel.Warning, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostClientWarning)]
        public void LogCompilerHostClientWarning(string requestId, string source, string message, string exceptionType, string exceptionDetails)
        {
            WriteDiagnosticsEvent(2602, requestId, source, message, exceptionType, exceptionDetails);
        }

        #endregion

        #region Data Provider Events (ID Range : 3000 - 3999)

        /// <summary>
        /// Log data provider message.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        [Event(3000, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogDataProviderMessage)]
        public void LogDataProviderMessage(string requestId, string source, string message)
        {
            WriteDiagnosticsEvent(
                3000,
                requestId,
                source,
                message);
        }

        /// <summary>
        /// Log data provider exception.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTime">End time.</param>
        /// <param name="latencyInMilliseconds">The latency.</param>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="exceptionDetails">Exception details.</param>
        [Event(3001, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogDataProviderException)]
        public void LogDataProviderException(string requestId, string source, string startTime, string endTime, long latencyInMilliseconds, string exceptionType, string exceptionDetails)
        {
            WriteDiagnosticsEvent(
                3001,
                requestId,
                source,
                startTime,
                endTime,
                latencyInMilliseconds,
                exceptionType,
                exceptionDetails);
        }

        /// <summary>
        /// Log data provider operation summary.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="source">The source.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="latencyInMilliseconds">The latency.</param>
        [Event(3002, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogDataProviderOperationSummary)]
        public void LogDataProviderOperationSummary(string requestId, string source, string startTime, string endTime, long latencyInMilliseconds)
        {
            WriteDiagnosticsEvent(
                3002,
                requestId,
                source,
                startTime,
                endTime,
                latencyInMilliseconds);
        }

        /// <summary>
        /// Log Kusto token refresh summary.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="message">The message.</param>
        /// <param name="latencyInMilliseconds">The latency.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="exceptionDetails">Exception details.</param>
        [Event(3003, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogKustoTokenRefreshSummary)]
        public void LogKustoTokenRefreshSummary(string source, string message, long latencyInMilliseconds, string startTime, string endTime, string exceptionType, string exceptionDetails)
        {
            WriteDiagnosticsEvent(
                3003,
                source,
                message,
                latencyInMilliseconds,
                startTime,
                endTime,
                exceptionType,
                exceptionDetails);
        }

        /// <summary>
        /// Log kusto query information.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="requestId">Request id.</param>
        /// <param name="message">The message.</param>
        /// <param name="latencyInMilliseconds">The latency.</param>
        /// <param name="details">The details.</param>
        /// <param name="content">The content.</param>
        /// <param name="exceptionType">Exception type.</param>
        /// <param name="exceptionDetails">Exception details.</param>
        [Event(3004, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogKustoQueryInformation)]
        public void LogKustoQueryInformation(string source, string requestId, string message, long latencyInMilliseconds, string details, string content, string exceptionType, string exceptionDetails)
        {
            WriteDiagnosticsEvent(
                3004,
                source,
                requestId,
                message,
                latencyInMilliseconds,
                details,
                content,
                exceptionType,
                exceptionDetails);
        }

        #endregion
    }
}
