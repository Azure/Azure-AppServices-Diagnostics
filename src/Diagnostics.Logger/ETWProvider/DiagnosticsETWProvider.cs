// <copyright file="DiagnosticsETWProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace Diagnostics.Logger
{
    /// <summary>
    /// Diagnostics ETW provider.
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-AppService-Diagnostics")]
    public sealed class DiagnosticsETWProvider : DiagnosticsEventSourceBase
    {
        private static readonly string EnvironmentName;
        private static readonly string WebsiteHostName;
        private static readonly Lazy<DiagnosticsETWProvider> _instance = new Lazy<DiagnosticsETWProvider>(() => new DiagnosticsETWProvider());

        static DiagnosticsETWProvider()
        {
            EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            WebsiteHostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME2");
        }

        /// <summary>
        /// ETW provider instance.
        /// </summary>
        public static DiagnosticsETWProvider Instance { get { return _instance.Value; } }

        #region Compile Host Events (ID Range : 1000 - 1999)

        /// <summary>
        /// Log compiler host message.
        /// </summary>
        /// <param name="Message">The message.</param>
        [Event(1000, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostMessage)]
        public void LogCompilerHostMessage(string Message, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(1000, Message, EnvironmentName, WebsiteHostName);
        }

        /// <summary>
        /// Log compiler host unhandled exception.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(1001, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostUnhandledException)]
        public void LogCompilerHostUnhandledException(string RequestId, string Source, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                1001,
                RequestId,
                Source,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log compiler host API summary.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Address">The address.</param>
        /// <param name="Verb">The verb.</param>
        /// <param name="StatusCode">The status code.</param>
        /// <param name="LatencyInMilliseconds">The latency.</param>
        /// <param name="StartTime">The start time.</param>
        /// <param name="EndTime">The end time.</param>
        [Event(1002, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostAPISummary)]
        public void LogCompilerHostAPISummary(string RequestId, string Address, string Verb, int StatusCode, long LatencyInMilliseconds, string StartTime, string EndTime, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                1002,
                RequestId,
                Address,
                Verb,
                StatusCode,
                LatencyInMilliseconds,
                StartTime,
                EndTime,
                EnvironmentName, 
                WebsiteHostName);
        }

        #endregion Compile Host Events (ID Range : 1000 - 1999)

        #region Runtime Host Events (ID Range : 2000 - 2499)

        /// <summary>
        /// Log runtime host message.
        /// </summary>
        /// <param name="Message">The message.</param>
        [Event(2000, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostMessage)]
        public void LogRuntimeHostMessage(string Message, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(2000, Message, EnvironmentName, WebsiteHostName);
        }

        /// <summary>
        /// Log runtime host unhandled exception.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="SubscriptionId">The subscription id.</param>
        /// <param name="ResourceGroup">The resource group.</param>
        /// <param name="Resource">The resource.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(2001, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostUnhandledException)]
        public void LogRuntimeHostUnhandledException(string RequestId, string Source, string SubscriptionId, string ResourceGroup, string Resource, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                2001,
                RequestId,
                Source,
                SubscriptionId,
                ResourceGroup,
                Resource,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log runtime host API summary.
        /// </summary>
        /// <param name="RequestId">The request id.</param>
        /// <param name="SubscriptionId">The subscription id.</param>
        /// <param name="ResourceGroup">The resource group.</param>
        /// <param name="Resource">The resource.</param>
        /// <param name="Address">The address.</param>
        /// <param name="Verb">The verb.</param>
        /// <param name="OperationName">Operation time.</param>
        /// <param name="StatusCode">Status code.</param>
        /// <param name="LatencyInMilliseconds">The latency.</param>
        /// <param name="StartTime">The start time.</param>
        /// <param name="EndTime">The end time.</param>
        /// <param name="Content">The headers content received.</param>
        [Event(2002, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostAPISummary)]
        public void LogRuntimeHostAPISummary(string RequestId, string SubscriptionId, string ResourceGroup, string Resource, string Address, string Verb, string OperationName, int StatusCode, long LatencyInMilliseconds, string StartTime, string EndTime, string Content, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                2002,
                RequestId,
                SubscriptionId,
                ResourceGroup,
                Resource,
                Address,
                Verb,
                OperationName,
                StatusCode,
                LatencyInMilliseconds,
                StartTime,
                EndTime,
                Content,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log retry attempt message.
        /// </summary>
        /// <param name="RequestId">The request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        [Event(2003, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRetryAttemptMessage)]
        public void LogRetryAttemptMessage(string RequestId, string Source, string Message, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                2003,
                RequestId,
                Source,
                Message,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log retry attempt summary.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        /// <param name="LatencyInMilliseconds">The latency.</param>
        /// <param name="StartTime">The start time.</param>
        /// <param name="EndTime">The end time.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(2004, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRetryAttemptSummary)]
        public void LogRetryAttemptSummary(string RequestId, string Source, string Message, long LatencyInMilliseconds, string StartTime, string EndTime, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                2004,
                RequestId,
                Source,
                Message,
                LatencyInMilliseconds,
                StartTime,
                EndTime,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log runtime host insight correlation.
        /// </summary>
        /// <param name="RequestId">The request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="SubscriptionId">The subscription id.</param>
        /// <param name="ResourceGroup">The resource group.</param>
        /// <param name="Resource">The resource.</param>
        /// <param name="CorrelationId">The correlation id.</param>
        /// <param name="Message">The message.</param>
        [Event(2005, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostInsightsCorrelation, Version = 2)]
        public void LogRuntimeHostInsightCorrelation(string RequestId, string Source, string SubscriptionId, string ResourceGroup, string Resource, string CorrelationId, string Message, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                2005,
                RequestId,
                Source,
                SubscriptionId,
                ResourceGroup,
                Resource,
                CorrelationId,
                Message,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log runtime host handled exception.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="SubscriptionId">Subscription id.</param>
        /// <param name="ResourceGroup">Resource group.</param>
        /// <param name="Resource">The resource.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(2006, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostHandledException)]
        public void LogRuntimeHostHandledException(string RequestId, string Source, string SubscriptionId, string ResourceGroup, string Resource, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                2006,
                RequestId,
                Source,
                SubscriptionId,
                ResourceGroup,
                Resource,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log full ASC insight.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        /// <param name="Details">The details.</param>
        [Event(2008, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogRuntimeHostSupportTopicAscInsight)]
        public void LogFullAscInsight(string RequestId, string Source, string Message, string Details, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                2008,
                RequestId,
                Source,
                Message,
                Details,
                EnvironmentName, 
                WebsiteHostName);
        }

        #endregion Runtime Host Events (ID Range : 2000 - 2499)

        #region SourceWatcher Events (ID Range : 2500 - 2599)

        /// <summary>
        /// Log source watch message.
        /// </summary>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        [Event(2500, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogSourceWatcherMessage)]
        public void LogSourceWatcherMessage(string Source, string Message, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(2500, Source, Message, EnvironmentName, WebsiteHostName);
        }

        /// <summary>
        /// Log source watcher warning.
        /// </summary>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        [Event(2501, Level = EventLevel.Warning, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogSourceWatcherWarning)]
        public void LogSourceWatcherWarning(string Source, string Message, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(2501, Source, Message, EnvironmentName, WebsiteHostName);
        }

        /// <summary>
        /// Log source watcher exception.
        /// </summary>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(2502, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogSourceWatcherException)]
        public void LogSourceWatcherException(string Source, string Message, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(2502, Source, Message, ExceptionType, ExceptionDetails, EnvironmentName, WebsiteHostName);
        }

        #endregion SourceWatcher Events (ID Range : 2500 - 2599)

        #region Compiler Host Client Events (ID Range: 2600 - 2699)

        /// <summary>
        /// Log compiler host client message.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        [Event(2600, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostClientMessage)]
        public void LogCompilerHostClientMessage(string RequestId, string Source, string Message, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(2600, RequestId, Source, Message, EnvironmentName, WebsiteHostName);
        }

        /// <summary>
        /// Log compiler host client exception.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(2601, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostClientException)]
        public void LogCompilerHostClientException(string RequestId, string Source, string Message, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(2601, RequestId, Source, Message, ExceptionType, ExceptionDetails, EnvironmentName, WebsiteHostName);
        }

        /// <summary>
        /// Log compiler host client warning.
        /// </summary>
        /// <param name="RequestId">The request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(2602, Level = EventLevel.Warning, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogCompilerHostClientWarning)]
        public void LogCompilerHostClientWarning(string RequestId, string Source, string Message, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(2602, RequestId, Source, Message, ExceptionType, ExceptionDetails, EnvironmentName, WebsiteHostName);
        }

        #endregion Compiler Host Client Events (ID Range: 2600 - 2699)

        #region Data Provider Events (ID Range : 3000 - 3999)

        /// <summary>
        /// Log data provider message.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        [Event(3000, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogDataProviderMessage)]
        public void LogDataProviderMessage(string RequestId, string Source, string Message, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                3000,
                RequestId,
                Source,
                Message,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log data provider exception.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="StartTime">Start time.</param>
        /// <param name="EndTime">End time.</param>
        /// <param name="LatencyInMilliseconds">The latency.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(3001, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogDataProviderException)]
        public void LogDataProviderException(string RequestId, string Source, string StartTime, string EndTime, long LatencyInMilliseconds, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                3001,
                RequestId,
                Source,
                StartTime,
                EndTime,
                LatencyInMilliseconds,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log data provider operation summary.
        /// </summary>
        /// <param name="RequestId">The request id.</param>
        /// <param name="Source">The source.</param>
        /// <param name="StartTime">The start time.</param>
        /// <param name="EndTime">The end time.</param>
        /// <param name="LatencyInMilliseconds">The latency.</param>
        [Event(3002, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogDataProviderOperationSummary)]
        public void LogDataProviderOperationSummary(string RequestId, string Source, string StartTime, string EndTime, long LatencyInMilliseconds, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                3002,
                RequestId,
                Source,
                StartTime,
                EndTime,
                LatencyInMilliseconds,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log Kusto token refresh summary.
        /// </summary>
        /// <param name="Source">The source.</param>
        /// <param name="Message">The message.</param>
        /// <param name="LatencyInMilliseconds">The latency.</param>
        /// <param name="StartTime">The start time.</param>
        /// <param name="EndTime">The end time.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(3003, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogTokenRefreshSummary)]
        public void LogTokenRefreshSummary(string Source, string Message, long LatencyInMilliseconds, string StartTime, string EndTime, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                3003,
                Source,
                Message,
                LatencyInMilliseconds,
                StartTime,
                EndTime,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log kusto query information.
        /// </summary>
        /// <param name="Source">The source.</param>
        /// <param name="RequestId">Request id.</param>
        /// <param name="Message">The message.</param>
        /// <param name="LatencyInMilliseconds">The latency.</param>
        /// <param name="Details">The details.</param>
        /// <param name="Content">The content.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(3004, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogKustoQueryInformation)]
        public void LogKustoQueryInformation(string Source, string RequestId, string Message, long LatencyInMilliseconds, string Details, string Content, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                3004,
                Source,
                RequestId,
                Message,
                LatencyInMilliseconds,
                Details,
                Content,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log kusto query information.
        /// </summary>
        /// <param name="ActivityId">Activity id.</param>
        /// <param name="Message">The message.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(3005, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogKustoHeartbeatInformation)]
        public void LogKustoHeartbeatInformation(string ActivityId, string Message, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                3005,
                ActivityId,
                Message,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        #endregion Data Provider Events (ID Range : 3000 - 3999)

        #region Internal AI API Events (ID Range : 4000 - 4199)

        /// <summary>
        /// Log Internal AI API message.
        /// </summary>
        /// <param name="Message">The message.</param>
        [Event(4000, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogInternalAPIMessage)]
        public void LogInternalAPIMessage(string Message, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(4000, Message, EnvironmentName, WebsiteHostName);
        }

        /// <summary>
        /// Log Internal AI API unhandled exception.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(4001, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogInternalAPIUnhandledException)]
        public void LogInternalAPIUnhandledException(string RequestId, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                4001,
                RequestId,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log Internal API summary.
        /// </summary>
        /// <param name="RequestId">The request id.</param>
        /// <param name="OperationName">Operation time.</param>
        /// <param name="StatusCode">Status code.</param>
        /// <param name="LatencyInMilliseconds">The latency.</param>
        /// <param name="StartTime">The start time.</param>
        /// <param name="EndTime">The end time.</param>
        /// <param name="Content">The headers content received.</param>
        [Event(4002, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogInternalAPISummary)]
        public void LogInternalAPISummary(string RequestId, string OperationName, int StatusCode, long LatencyInMilliseconds, string StartTime, string EndTime, string Content, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                4002,
                RequestId,
                OperationName,
                StatusCode,
                LatencyInMilliseconds,
                StartTime,
                EndTime,
                Content,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log Internal API Insights.
        /// </summary>
        /// <param name="RequestId">The request id.</param>
        /// <param name="Message">The message.</param>
        [Event(4005, Level = EventLevel.Informational, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogInternalAPIInsights, Version = 2)]
        public void LogInternalAPIInsights(string RequestId, string Message, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                4005,
                RequestId,
                Message,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log Internal API handled exception.
        /// </summary>
        /// <param name="RequestId">Request id.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(4006, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogInternalAPIHandledException)]
        public void LogInternalAPIHandledException(string RequestId, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                4006,
                RequestId,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log Internal API Training Exception.
        /// </summary>
        /// <param name="RequestId">Request Id.</param>
        /// <param name="TrainingId">Training id.</param>
        /// <param name="ProductId">Product id.</param>
        /// <param name="ExceptionType">Exception type.</param>
        /// <param name="ExceptionDetails">Exception details.</param>
        [Event(4020, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogInternalAPITrainingException)]
        public void LogInternalAPITrainingException(string RequestId, string TrainingId, string ProductId, string ExceptionType, string ExceptionDetails, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                4020,
                TrainingId,
                ProductId,
                ExceptionType,
                ExceptionDetails,
                EnvironmentName, 
                WebsiteHostName);
        }

        /// <summary>
        /// Log Internal API Training Summary.
        /// </summary>
        /// <param name="RequestId">Request Id.</param>
        /// <param name="TrainingId">Training id.</param>
        /// <param name="ProductId">Product id.</param>
        /// <param name="LatencyInMilliseconds">The latency.</param>
        /// <param name="StartTime">Start Time</param>
        /// <param name="EndTime">End Time</param>
        /// <param name="Content">Summary details.</param>
        [Event(4021, Level = EventLevel.Error, Channel = EventChannel.Admin, Message = ETWMessageTemplates.LogInternalAPITrainingSummary)]
        public void LogInternalAPITrainingSummary(string RequestId, string TrainingId, string ProductId, long LatencyInMilliseconds, string StartTime, string EndTime, string Content, string DiagEnvironment = null, string DiagWebsiteHostName = null)
        {
            WriteDiagnosticsEvent(
                4021,
                TrainingId,
                ProductId,
                LatencyInMilliseconds,
                StartTime,
                EndTime,
                Content,
                EnvironmentName, 
                WebsiteHostName);
        }

        #endregion Internal AI API Events (ID Range : 4000 - 4199)
    }
}
