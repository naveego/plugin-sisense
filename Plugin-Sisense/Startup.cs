using System;
using System.IO;
using System.Linq;
using System.Threading;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Plugin_Sisense.API.Replication;
using Plugin_Sisense.Configuration;
using Plugin_Sisense.Helper;
using Plugin_Sisense.Plugin;
using IApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Plugin_Sisense
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
            // Swagger
            services.ConfigureSwaggerServices(Configuration);
            
            // Api Config
            services.AddRouting(options => { options.LowercaseUrls = true; });
            services.AddMvcCore()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddVersionedApiExplorer(o =>
                {
                    o.DefaultApiVersion = new ApiVersion(1, 0);
                    o.SubstituteApiVersionInUrl = true;
                    o.GroupNameFormat = "'v'V";
                })
                .AddApiExplorer()
                .AddAuthorization()
                .AddFormatterMappings()
                .AddDataAnnotations()
                .AddControllersAsServices();

            services.AddControllers()
                .AddNewtonsoftJson(o =>
                {
                    o.SerializerSettings.Converters.Add(new StringEnumConverter
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    });
                });

            services.AddApiVersioning(o => { o.ReportApiVersions = true; });
            
            services.AddSingleton<IHostedService, GetBindingHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.ConfigureSwagger();
            
            GetBindingHostedService.ServerAddresses = app.ServerFeatures.Get<IServerAddressesFeature>();

            applicationLifetime.ApplicationStarted.Register(Program.Run);
        }
    }
}