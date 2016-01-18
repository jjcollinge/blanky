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
using Newtonsoft.Json;

namespace MicroserviceTemplate
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
            services.AddLogging();
            services.AddMvcCore()
                    .AddJsonFormatters();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.Map("/health", subApp =>
            {
                subApp.Use(async (context, next) =>
               {
                   IEnumerable<Type> healthTypes =
                                    AppDomain
                                   .CurrentDomain
                                   .GetAssemblies()
                                   .SelectMany(assembly => assembly.GetTypes())
                                   .Where(type => typeof(IHealth).IsAssignableFrom(type)
                                    && type.IsAbstract == false
                                    && type.IsInterface == false
                                    && type.IsGenericTypeDefinition == false);

                   var servicesHealth = new Dictionary<string, string>();

                   foreach (var healthType in healthTypes)
                   {
                       var healthService = (IHealth)Activator.CreateInstance(healthType);
                       var serviceHealth = await healthService.Check();
                       servicesHealth.Add(healthType.ToString(), serviceHealth);
                   }

                   var jsonResponse = JsonConvert.SerializeObject(servicesHealth, Formatting.Indented);
                   await context.Response.WriteAsync(jsonResponse);
               });
            });
        }
    }
}
