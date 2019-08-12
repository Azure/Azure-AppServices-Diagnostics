// <copyright file="Startup.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using Diagnostics.CompilerHost.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;

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
        public Startup(IHostingEnvironment hostingEnvironment)
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true)
              .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", optional: true)
              .AddEnvironmentVariables();

            Configuration = builder.Build();
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
            string openIdConfigEndpoint = $"{Configuration["AzureAd:AADAuthority"]}/.well-known/openid-configuration";
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(openIdConfigEndpoint, new OpenIdConnectConfigurationRetriever());
            var config = configManager.GetConfigurationAsync().Result;
            var issuer = config.Issuer;
            var signingKeys = config.SigningKeys;
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidAudience = Configuration["AzureAd:ClientId"],
                        ValidateIssuer = true,
                        ValidIssuers = new[] { issuer, $"{issuer}/v2.0" },
                        ValidateLifetime = true,
                        RequireSignedTokens = true,
                        IssuerSigningKeys = signingKeys
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            var allowedAppIds = Configuration["AzureAd:AllowedAppIds"].Split(",").Select(p => p.Trim()).ToList();
                            var claimPrincipal = context.Principal;
                            var appId = claimPrincipal.Claims.FirstOrDefault(c => c.Type.Equals("appid", StringComparison.CurrentCultureIgnoreCase));
                            if (appId == null || !allowedAppIds.Exists(p => p.Equals(appId.Value, StringComparison.OrdinalIgnoreCase)))
                            {
                                context.Fail("Unauthorized Request");
                            }
                            return Task.CompletedTask;
                        }
                    };

                });
            // Enable app insights telemetry
            services.AddApplicationInsightsTelemetry();
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
            app.UseAuthentication();
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
