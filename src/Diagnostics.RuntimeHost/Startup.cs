using Diagnostics.DataProviders;
using Diagnostics.DataProviders.TokenService;
using Diagnostics.RuntimeHost.Middleware;
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
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

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
            // Enable App Insights telemetry
            services.AddApplicationInsightsTelemetry();
            services.AddMvc();

            stopwatch.Stop();
            Console.WriteLine("azure config 1 loaded: " + stopwatch.ElapsedMilliseconds + "ms");
            stopwatch.Restart();


            services.AddSingleton<IDataSourcesConfigurationService, DataSourcesConfigurationService>();
            services.AddSingleton<ICompilerHostClient, CompilerHostClient>();
            services.AddSingleton<ISourceWatcherService, SourceWatcherService>();
            services.AddSingleton<IInvokerCacheService, InvokerCacheService>();
            services.AddSingleton<IGistCacheService, GistCacheService>();
            services.AddSingleton<ISiteService, SiteService>();
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

            bool searchIsEnabled = Convert.ToBoolean(Configuration[$"SearchAPI:{RegistryConstants.SearchAPIEnabledKey}"]);

            if (searchIsEnabled)
            {
                services.AddSingleton<ISearchService, SearchService>();
            }
            else
            {
                services.AddSingleton<ISearchService, SearchServiceDisabled>();
            }

            services.AddSingleton<IKustoHeartBeatService>(new KustoHeartBeatService(kustoConfiguration));

            stopwatch.Stop();
            Console.WriteLine("azure config 3B loaded: " + stopwatch.ElapsedMilliseconds + "ms");
            stopwatch.Restart();

            observerConfiguration.AADAuthority = dataSourcesConfigService.Config.KustoConfiguration.AADAuthority;
            var wawsObserverTokenService = new ObserverTokenService(observerConfiguration.WawsObserverResourceId, observerConfiguration);
            var supportBayApiObserverTokenService = new ObserverTokenService(observerConfiguration.SupportBayApiObserverResourceId, observerConfiguration);
            services.AddSingleton<IWawsObserverTokenService>(wawsObserverTokenService);
            services.AddSingleton<ISupportBayApiObserverTokenService>(supportBayApiObserverTokenService);

            stopwatch.Stop();
            Console.WriteLine("azure config 4A loaded: " + stopwatch.ElapsedMilliseconds + "ms");
            stopwatch.Restart();

            KustoTokenService.Instance.Initialize(dataSourcesConfigService.Config.KustoConfiguration);
            ChangeAnalysisTokenService.Instance.Initialize(dataSourcesConfigService.Config.ChangeAnalysisDataProviderConfiguration);
            AscTokenService.Instance.Initialize(dataSourcesConfigService.Config.AscDataProviderConfiguration);
            if (searchIsEnabled)
            {
                SearchServiceTokenService.Instance.Initialize(dataSourcesConfigService.Config.SearchServiceProviderConfiguration);
            }
            CompilerHostTokenService.Instance.Initialize(Configuration);

            if (Environment.IsProduction())
            {
                GeoCertLoader.Instance.Initialize(Configuration);
                MdmCertLoader.Instance.Initialize(Configuration);
            }

            stopwatch.Stop();
            Console.WriteLine("azure config 4B loaded: " + stopwatch.ElapsedMilliseconds + "ms");
            stopwatch.Restart();

            // Initialize on startup
            servicesProvider.GetService<ISourceWatcherService>();

            stopwatch.Stop();
            Console.WriteLine("azure config 5 loaded: " + stopwatch.ElapsedMilliseconds + "ms");
        }

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
