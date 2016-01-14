using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Fabric;
using System.Fabric.Query;
using Newtonsoft.Json;

namespace ServiceRouter
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json")
                                                      .AddEnvironmentVariables()
                                                      .Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //// Add framework services.
            //services.AddMvcCore()
            //        .AddJsonFormatters();

            services.AddSingleton<FabricClient>(new FabricClient());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, FabricClient client)
        {
            app.Use(async (context, next) =>
            {
                var services = new List<ServiceList>();
                var applications = await client.QueryManager.GetApplicationListAsync();
                foreach(var application in applications)
                {
                    services.Add(await client.QueryManager.GetServiceListAsync(application.ApplicationName));
                }

                var servicesUrisInCluster = services.SelectMany(x => x.Select(y => y.ServiceName));

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(servicesUrisInCluster, Formatting.Indented);

                await context.Response.WriteAsync("Hello from gateway! We've got:" + json );
            });
        }
    }
}
