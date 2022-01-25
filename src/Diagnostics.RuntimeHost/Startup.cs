using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.DataProviders.TokenService;
using Diagnostics.RuntimeHost.Middleware;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Security.CertificateAuth;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json;
using Diagnostics.RuntimeHost.Services.SourceWatcher.Watchers;
using Diagnostics.Logger;
using Microsoft.Extensions.Hosting;
using Diagnostics.RuntimeHost.Services.DiagnosticsTranslator;
using Diagnostics.RuntimeHost.Services.DevOpsClient;
using System.Text.Json.Serialization;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.RuntimeHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
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
        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            ValidateSecuritySettings();
            IdentityModelEventSource.ShowPII = Configuration.GetValue("ShowIdentityModelErrors", false);
            var openIdConfigEndpoint = $"{Configuration["SecuritySettings:AADAuthority"]}/.well-known/openid-configuration";
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(openIdConfigEndpoint, new OpenIdConnectConfigurationRetriever());
            var config = configManager.GetConfigurationAsync().Result;
            var issuer = config.Issuer;
            var signingKeys = config.SigningKeys;
            // Adding both custom cert auth handler and Azure AAD JWT token handler to support multiple forms of auth.
            if (Environment.IsProduction() || Environment.IsStaging())
            {
                services.AddAuthentication().AddCertificateAuth(CertificateAuthDefaults.AuthenticationScheme,
                    options =>
                    {
                        options.AllowedIssuers = Configuration["SecuritySettings:AllowedCertIssuers"].Split("|").Select(p => p.Trim()).ToList();
                        var allowedSubjectNames = Configuration["SecuritySettings:AllowedCertSubjectNames"].Split(",").Select(p => p.Trim()).ToList();
                        options.AllowedSubjectNames = allowedSubjectNames;
                    }).AddJwtBearer("AzureAd", options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudiences =  new[] { Configuration["SecuritySettings:ClientId"], $"spn:{Configuration["SecuritySettings:ClientId"]}" },
                        ValidIssuers = new[] { issuer, $"{issuer}/v2.0" },
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
                                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"AAD Authentication failed because incoming app id was not allowed");
                            }
                            var allowedDeploymentIds = Configuration["SecuritySettings:AllowedDeploymentIds"].Split(",").Select(p => p.Trim()).ToList();
                            var path = context.Request.Path;
                            if (incomingAppId != null && allowedDeploymentIds.Exists(p => p.Equals(incomingAppId.Value, StringComparison.OrdinalIgnoreCase))
                             && !path.Value.EndsWith("api/deploy", StringComparison.CurrentCultureIgnoreCase))
                            {
                                context.Fail("Unauthorized Request");
                                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"The app id {incomingAppId} does not have permission to this API {path}");
                            }
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"AAD Authentication failure reason: {context.Exception.ToString()}");
                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                           
                            context.Request.Headers.TryGetValue("Authorization", out var BearerToken);
                            if (BearerToken.Count == 0)
                            {
                                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage("No bearer token was sent");
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
                CompilerHostCertLoader.Instance.Initialize(Configuration);
                SearchAPICertLoader.Instance.Initialize(Configuration);
                // Enable App Insights telemetry
                services.AddApplicationInsightsTelemetry();
            }

            if (Configuration.GetValue("ContainerAppsMdm:Enabled", true))
            {
                ContainerAppsMdmCertLoader.Instance.Initialize(Configuration);
            }

            services.AddMemoryCache();
            services.AddAppServiceApplicationLogging();

            if (Environment.IsDevelopment())
            {
                services.AddControllers(options =>
                {
                    options.Filters.Add<AllowAnonymousFilter>();
                }).AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.Converters.Add(new ExceptionConverter());
                    options.JsonSerializerOptions.Converters.Add(new AsRuntimeTypeConverter<Rendering>());
                    options.JsonSerializerOptions.Converters.Add(new AsRuntimeTypeConverter<ModelsAndUtils.Models.ResponseExtensions.FormInputBase>());
                    options.JsonSerializerOptions.Converters.Add(new DevOpsGetBranchesConverter());
                    options.JsonSerializerOptions.Converters.Add(new DevOpsMakePRConverter());
                });
            }
            else
            {
                services.AddControllers().AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.Converters.Add(new ExceptionConverter());
                    options.JsonSerializerOptions.Converters.Add(new AsRuntimeTypeConverter<Rendering>());
                    options.JsonSerializerOptions.Converters.Add(new AsRuntimeTypeConverter<ModelsAndUtils.Models.ResponseExtensions.FormInputBase>());
                    options.JsonSerializerOptions.Converters.Add(new DevOpsGetBranchesConverter());
                    options.JsonSerializerOptions.Converters.Add(new DevOpsMakePRConverter());
                });
            }

            services.AddSingleton<IDataSourcesConfigurationService, DataSourcesConfigurationService>();
            services.AddSingleton<ICompilerHostClient, CompilerHostClient>();
            if (Configuration.IsAirGappedCloud())
            {
                services.AddSingleton<IGithubClient, AzureStorageSourceCodeClient>();
            }
            else
            {
                services.AddSingleton<IGithubClient, GithubClient>();
            }

            if (Configuration.IsAirGappedCloud() || Configuration.IsAzureUSGovernment() || Configuration.IsAzureChinaCloud())
            {
                services.AddSingleton<IRepoClient, NationalCloudDevOpsClient>();
            }
            else
            {
                services.AddSingleton<IRepoClient, DevOpsClient>();
            }
            services.AddSingleton<ISourceWatcherService, SourceWatcherService>();
            services.AddSingleton<IInvokerCacheService, InvokerCacheService>();
            services.AddSingleton<IGistCacheService, GistCacheService>();
            services.AddSingleton<IKustoMappingsCacheService, KustoMappingsCacheService>();
            services.AddSingleton<ITranslationCacheService, TranslationCacheService>();
            services.AddSingleton<ISiteService, SiteService>();
            services.AddSingleton<ISupportTopicService, SupportTopicService>();
            services.AddScoped(typeof(IRuntimeContext<>), typeof(RuntimeContext<>));
            services.AddSingleton<IDiagnosticTranslatorService, DiagnosticTranslatorService>();
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

            var autoHealMonitoringServiceInstance = new AuoHealMonitoringService(Configuration, Environment);
            services.AddSingleton<IAutoHealMonitoringService>(autoHealMonitoringServiceInstance);

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

            if (Configuration.GetValue("K8SELogAnalytics:Enabled", true))
            {
                K8SELogAnalyticsTokenService.Instance.Initialize(dataSourcesConfigService.Config.K8SELogAnalyticsConfiguration);
            }

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

            services.AddDiagEntitiesStorageService(Configuration);
            services.AddDiagEntitiesTableCacheService(Configuration);

            InjectSourceWatcher(services);
            services.AddLogging(loggingConfig =>
            {
                loggingConfig.ClearProviders();
                loggingConfig.AddConfiguration(Configuration.GetSection("Logging"));
                loggingConfig.AddApplicationInsights();
                loggingConfig.AddEventSourceLogger();
                loggingConfig.AddRuntimeLogger();
             
                if (Environment.IsDevelopment())
                {
                    loggingConfig.AddDebug();
                    loggingConfig.AddConsole();
                }

                if (Configuration.IsAirGappedCloud())
                {
                    loggingConfig.AddEventLog();
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHttpsRedirection();
            // URL Rewrite middleware should be before app.UseRouting() for it to work.
            app.UseRewriter(new RewriteOptions().Add(new RewriteDiagnosticResource()));        
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<DiagnosticsRequestMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                if (env.IsDevelopment())
                {
                    endpoints.MapControllers().WithMetadata(new AllowAnonymousAttribute());
                } else
                {
                    endpoints.MapControllers();
                }               
            });
        }

        /// <summary>
        /// Inject appropriate SourceWatcher based on CloudDomain
        /// </summary>
        /// <param name="services"></param>
        private void InjectSourceWatcher(IServiceCollection services)
        {
            if (Configuration.IsPublicAzure() || Configuration.IsAirGappedCloud())
            {
                services.AddSingleton<ISourceWatcher, StorageWatcher>();
            }
            if(Configuration.IsAzureChinaCloud() || Configuration.IsAzureUSGovernment())
            {
                services.AddSingleton<ISourceWatcher, NationalCloudStorageWatcher>();
            }
        }

        private void ValidateSecuritySettings()
        {
            var securitySettings = Configuration.GetSection("SecuritySettings").GetChildren();
            foreach( var setting in securitySettings)
            {
                if (setting.Value == null)
                {
                    throw new Exception($"Configuration {setting.Key} cannot be null");
                }
            }
        }
    }
}
