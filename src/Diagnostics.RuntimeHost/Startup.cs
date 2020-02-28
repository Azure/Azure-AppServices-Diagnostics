using Diagnostics.DataProviders;
using Diagnostics.DataProviders.TokenService;
using Diagnostics.RuntimeHost.Middleware;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Diagnostics.RuntimeHost.Security.CertificateAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Logging;
using System.Net;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Rewrite;

namespace Diagnostics.RuntimeHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;

            if (!Environment.IsProduction())
            {
                AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
                {
                    Debug.WriteLine(eventArgs.Exception.ToString());
                };
            }
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }


        public void ConfigureServices(IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = Configuration.GetValue("ShowIdentityModelErrors", false);
            var openIdConfigEndpoint = $"{Configuration["SecuritySettings:AADAuthority"]}/.well-known/openid-configuration";
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(openIdConfigEndpoint, new OpenIdConnectConfigurationRetriever());
            var config = configManager.GetConfigurationAsync().Result;
            var issuer = config.Issuer;
            var signingKeys = config.SigningKeys;
            // Adding both custom cert auth handler and Azure AAD JWT token handler to support multiple forms of auth.
            if (Environment.IsProduction() || Environment.IsStaging() )
            {
                services.AddAuthentication().AddCertificateAuth(CertificateAuthDefaults.AuthenticationScheme,
                    options =>
                    {
                        options.AllowedIssuers = Configuration["SecuritySettings:AllowedCertIssuers"].Split("|").Select(p => p.Trim()).ToList();
                        options.AllowedSubjectNames = Configuration["SecuritySettings:AllowedCertSubjectNames"].Split(",").Select(p => p.Trim()).ToList();
                    }).AddJwtBearer("AzureAd", options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidAudience = Configuration["SecuritySettings:ClientId"],
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
                            var allowedAppIds = Configuration["SecuritySettings:AllowedAppIds"].Split(",").Select(p => p.Trim()).ToList();
                            var claimPrincipal = context.Principal;
                            var incomingAppId = claimPrincipal.Claims.FirstOrDefault(c => c.Type.Equals("appid", StringComparison.CurrentCultureIgnoreCase));
                            if (incomingAppId == null || !allowedAppIds.Exists(p => p.Equals(incomingAppId.Value, StringComparison.OrdinalIgnoreCase)))
                            {
                                context.Fail("Unauthorized Request");
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder().
                                            RequireAuthenticatedUser().
                                            AddAuthenticationSchemes(CertificateAuthDefaults.AuthenticationScheme, "AzureAd")
                                            .Build();
                });
            }
            if (!Environment.IsDevelopment())
            {
                GeoCertLoader.Instance.Initialize(Configuration);
                MdmCertLoader.Instance.Initialize(Configuration);
            }

            // Enable App Insights telemetry
            services.AddApplicationInsightsTelemetry();
            services.AddAppServiceApplicationLogging();
            if(Environment.IsDevelopment())
            {
                services.AddMvc(options =>
                {
                    options.Filters.Add(new AllowAnonymousFilter());
                });
            }
            else
            {
                services.AddMvc();
            }

            services.AddSingleton<IDataSourcesConfigurationService, DataSourcesConfigurationService>();
            services.AddSingleton<ICompilerHostClient, CompilerHostClient>();
            services.AddSingleton<ISourceWatcherService, SourceWatcherService>();
            services.AddSingleton<IInvokerCacheService, InvokerCacheService>();
            services.AddSingleton<IGistCacheService, GistCacheService>();
            services.AddSingleton<IKustoMappingsCacheService, KustoMappingsCacheService>();
            services.AddSingleton<ISiteService, SiteService>();
            services.AddSingleton<ISupportTopicService, SupportTopicService>();
            services.AddScoped(typeof(IRuntimeContext<>), typeof(RuntimeContext<>));
            services.AddSingleton<IStampService>((serviceProvider) =>
            {
                var cloudDomain = serviceProvider.GetService<IDataSourcesConfigurationService>().Config.KustoConfiguration.CloudDomain;
                switch (cloudDomain)
                {
                    case DataProviderConstants.AzureChinaCloud:
                    case DataProviderConstants.AzureUSGovernment:
                    case DataProviderConstants.AzureUSNat:
                    case DataProviderConstants.AzureUSSec:
                        return new NationalCloudStampService();
                    default:
                        return new StampService();
                }
            });
            services.AddSingleton<IAssemblyCacheService, AssemblyCacheService>();
            services.AddSingleton<IHealthCheckService, HealthCheckService>();

            var servicesProvider = services.BuildServiceProvider();
            var dataSourcesConfigService = servicesProvider.GetService<IDataSourcesConfigurationService>();
            var observerConfiguration = dataSourcesConfigService.Config.SupportObserverConfiguration;
            var kustoConfiguration = dataSourcesConfigService.Config.KustoConfiguration;
            var searchApiConfiguration = dataSourcesConfigService.Config.SearchServiceProviderConfiguration;

            services.AddSingleton<IKustoHeartBeatService>(new KustoHeartBeatService(kustoConfiguration));

            observerConfiguration.AADAuthority = dataSourcesConfigService.Config.KustoConfiguration.AADAuthority;
            var wawsObserverTokenService = new ObserverTokenService(observerConfiguration.AADResource, observerConfiguration);
            var supportBayApiObserverTokenService = new ObserverTokenService(observerConfiguration.SupportBayApiObserverResourceId, observerConfiguration);
            services.AddSingleton<IWawsObserverTokenService>(wawsObserverTokenService);
            services.AddSingleton<ISupportBayApiObserverTokenService>(supportBayApiObserverTokenService);
            var observerServicePoint = ServicePointManager.FindServicePoint(new Uri(dataSourcesConfigService.Config.SupportObserverConfiguration.Endpoint));
            observerServicePoint.ConnectionLeaseTimeout = 60 * 1000;


            if (Configuration.GetValue("ChangeAnalysis:Enabled", true))
            {
                ChangeAnalysisTokenService.Instance.Initialize(dataSourcesConfigService.Config.ChangeAnalysisDataProviderConfiguration);
            }

            if (Configuration.GetValue("AzureSupportCenter:Enabled", true))
            {
                AscTokenService.Instance.Initialize(dataSourcesConfigService.Config.AscDataProviderConfiguration);
            }

            if (Configuration.GetValue("CompilerHost:Enabled", true))
            {
                CompilerHostTokenService.Instance.Initialize(Configuration);
            }

            if (searchApiConfiguration.Enabled || searchApiConfiguration.SearchAPIEnabled)
            {
                services.AddSingleton<ISearchService, SearchService>();
                SearchServiceTokenService.Instance.Initialize(dataSourcesConfigService.Config.SearchServiceProviderConfiguration);
            }
            else
            {
                services.AddSingleton<ISearchService, SearchServiceDisabled>();
            }
            // Initialize on startup
            servicesProvider.GetService<ISourceWatcherService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseAuthentication();
            app.UseRewriter(new RewriteOptions().Add(new RewriteDiagnosticResource()));
            app.UseMiddleware<DiagnosticsRequestMiddleware>();
            app.UseMvc();
        }
    }
}
