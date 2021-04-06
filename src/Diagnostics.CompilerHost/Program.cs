// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
                CreateHostBuilder(args).Build().Run();
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
        /// Builds Generic Host in 3.x
        /// </summary>
        /// <param name="args">The arguments</param>
        /// <returns>Hostbuilder</returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            return Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webbuilder =>
            {
                webbuilder.UseConfiguration(config);
                webbuilder.UseStartup<Startup>();
            });
        }

    }
}
