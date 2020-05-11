// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Diagnostics.Logger;
using System;

namespace Diagnostics.CompilerHost
{
    /// <summary>
    /// Class for program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The entry point.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            try
            {
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                //Log unhandled exceptions on startup
                DiagnosticsETWProvider.Instance.LogCompilerHostUnhandledException(
                    string.Empty,
                    "LogException_Startup",
                    ex.GetType().ToString(),
                    ex.ToString());

                throw;
            }
        }

        /// <summary>
        /// Build web host.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The web host.</returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            return
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(config)
                .UseStartup<Startup>();

        }
    }
}
