// <copyright file="DiagnosticsEventSourceBase.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text;

namespace Diagnostics.Logger
{
    /// <summary>
    /// Diagnostics event source base.
    /// </summary>
    public abstract class DiagnosticsEventSourceBase : EventSource
    {
        protected static string EnvironmentName;
        private static bool _traceOut;

        static DiagnosticsEventSourceBase()
        {
            EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            _traceOut = EnvironmentName.Equals("Development", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Write diagnostics event.
        /// </summary>
        /// <param name="eventId">Event id.</param>
        /// <param name="args">The args.</param>
        [NonEvent]
        protected void WriteDiagnosticsEvent(int eventId, params object[] args)
        {
            if (_traceOut)
            {
                var frame = new StackTrace().GetFrame(1); // Retrieve caller's method signature

                var sb = new StringBuilder($"EventId: {eventId}");
                var methodParams = frame.GetMethod().GetParameters();
                for (var index = 0; index < args?.Length && index < methodParams.Length; index++)
                {
                    sb.Append($", {methodParams[index].Name}: {args[index]}");
                }

                Trace.WriteLine(sb.ToString());
            }
            else
            {
                WriteEvent(eventId, args);
            }
        }
    }
}
