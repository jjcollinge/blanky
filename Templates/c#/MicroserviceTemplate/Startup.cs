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
using Swashbuckle.SwaggerGen;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.SwaggerGen.Generator;

namespace MicroserviceTemplate
{
    public class Startup
    {
        IHostingEnvironment _hostingEnv;
        IApplicationEnvironment _appEnv;

        public Startup(IHostingEnvironment hostingEnv, IApplicationEnvironment appEnv)
        {
            _hostingEnv = hostingEnv;
            _appEnv = appEnv;

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
            services.AddMvc();
            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //TODO: Add OAuth

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

            app.UseSwaggerGen();
            app.UseSwaggerUi();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
                
            });
        }
    }
}
