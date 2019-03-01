// <copyright file="Startup.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using Diagnostics.CompilerHost.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Diagnostics.CompilerHost
{
    /// <summary>
    /// Class for start up.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configure service.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <remarks>This method gets called by the runtime. Use this method to add services to the container.</remarks>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            CustomStartup();
        }

        /// <summary>
        /// Configure the app.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="env">The hosting environment.</param>
        /// <remarks>This method gets called by the runtime. Use this method to configure the HTTP request pipeline.</remarks>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCompilerRequestMiddleware();
            app.UseMvc();
        }

        private void CustomStartup()
        {
            // Execute a basic script to load roslyn successfully.
            var result = CSharpScript.EvaluateAsync<int>("1 + 2").Result;
        }
    }
}
