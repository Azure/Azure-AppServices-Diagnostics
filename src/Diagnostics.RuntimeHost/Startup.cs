using Diagnostics.DataProviders;
using Diagnostics.RuntimeHost.Middleware;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Diagnostics.RuntimeHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton<IDataSourcesConfigurationService, DataSourcesConfigurationService>();
            services.AddSingleton<ICompilerHostClient, CompilerHostClient>();
            services.AddSingleton<ISourceWatcherService, SourceWatcherService>();
            services.AddSingleton<IInvokerCacheService, InvokerCacheService>();
            services.AddSingleton<ISiteService, SiteService>();
            services.AddSingleton<IStampService, StampService>();

            // TODO : Not sure what's the right place for the following code piece.
            #region Custom Start up Code

            var servicesProvider = services.BuildServiceProvider();
            var dataSourcesConfigService = servicesProvider.GetService<IDataSourcesConfigurationService>();
            KustoTokenService.Instance.Initialize(dataSourcesConfigService.Config.KustoConfiguration);

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDiagnosticsRequestMiddleware();
            app.UseMvc();
        }
    }
}
