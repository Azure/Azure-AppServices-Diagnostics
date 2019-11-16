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
using Microsoft.Win32;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

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
            var openIdConfigEndpoint = $"{Configuration["SecuritySettings:AADAuthority"]}/.well-known/openid-configuration";
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(openIdConfigEndpoint, new OpenIdConnectConfigurationRetriever());
            var config = configManager.GetConfigurationAsync().Result;
            var issuer = config.Issuer;
            var signingKeys = config.SigningKeys;
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
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

            if (!Environment.IsDevelopment())
            {
                GeoCertLoader.Instance.Initialize(Configuration);
                MdmCertLoader.Instance.Initialize(Configuration);
            }

            // Enable App Insights telemetry
            services.AddApplicationInsightsTelemetry();
            services.AddMvc();

            services.AddSingleton<IDataSourcesConfigurationService, DataSourcesConfigurationService>();
            services.AddSingleton<ICompilerHostClient, CompilerHostClient>();
            services.AddSingleton<ISourceWatcherService, SourceWatcherService>();
            services.AddSingleton<IInvokerCacheService, InvokerCacheService>();
            services.AddSingleton<IGistCacheService, GistCacheService>();
            services.AddSingleton<ISiteService, SiteService>();
            services.AddScoped(typeof(IRuntimeContext<>), typeof(RuntimeContext<>));
            services.AddSingleton<IStampService>((serviceProvider) =>
            {
                var cloudDomain = serviceProvider.GetService<IDataSourcesConfigurationService>().Config.KustoConfiguration.CloudDomain;
                switch (cloudDomain)
                {
                    case DataProviderConstants.AzureChinaCloud:
                    case DataProviderConstants.AzureUSGovernment:
                        return new NationalCloudStampService();
                    default:
                        return new StampService();
                }
            });
            services.AddSingleton<IAssemblyCacheService, AssemblyCacheService>();

            var servicesProvider = services.BuildServiceProvider();
            var dataSourcesConfigService = servicesProvider.GetService<IDataSourcesConfigurationService>();
            var observerConfiguration = dataSourcesConfigService.Config.SupportObserverConfiguration;
            var kustoConfiguration = dataSourcesConfigService.Config.KustoConfiguration;
            var searchApiConfiguration = dataSourcesConfigService.Config.SearchServiceProviderConfiguration;

            services.AddSingleton<IKustoHeartBeatService>(new KustoHeartBeatService(kustoConfiguration));

            observerConfiguration.AADAuthority = dataSourcesConfigService.Config.KustoConfiguration.AADAuthority;
            var wawsObserverTokenService = new ObserverTokenService(observerConfiguration.WawsObserverResourceId, observerConfiguration);
            var supportBayApiObserverTokenService = new ObserverTokenService(observerConfiguration.SupportBayApiObserverResourceId, observerConfiguration);
            services.AddSingleton<IWawsObserverTokenService>(wawsObserverTokenService);
            services.AddSingleton<ISupportBayApiObserverTokenService>(supportBayApiObserverTokenService);

            ChangeAnalysisTokenService.Instance.Initialize(dataSourcesConfigService.Config.ChangeAnalysisDataProviderConfiguration);
            AscTokenService.Instance.Initialize(dataSourcesConfigService.Config.AscDataProviderConfiguration);
            CompilerHostTokenService.Instance.Initialize(Configuration);
            if (searchApiConfiguration.SearchAPIEnabled)
            {
                services.AddSingleton<ISearchService, SearchService>();
                SearchServiceTokenService.Instance.Initialize(dataSourcesConfigService.Config.SearchServiceProviderConfiguration);
            }
            else
            {
                services.AddSingleton<ISearchService, SearchServiceDisabled>();
            }
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
            app.UseMiddleware<DiagnosticsRequestMiddleware>();
            app.UseMvc();
        }
    }
}
