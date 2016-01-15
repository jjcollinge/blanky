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
using ServiceRouter.ServiceDiscovery;

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
            services.AddLogging();
            services.AddSingleton<FabricClient>();
            services.AddSingleton<Resolver>();
        }

        public void ConfigureDebugServices(IServiceCollection services)
        {
            var clientName = System.Net.Dns.GetHostName();
            var client = new FabricClient(new FabricClientSettings
            {
                ClientFriendlyName = clientName,
                ConnectionInitializationTimeout = TimeSpan.FromSeconds(3),
                KeepAliveInterval = TimeSpan.FromSeconds(15),
            }, "localhost:19000");

            client.ClientConnected += Client_ClientConnected;
            client.ClientDisconnected += Client_ClientDisconnected;


            services.AddLogging();
            services.AddSingleton<FabricClient>(client);
            services.AddSingleton<Resolver>();
        }

        private void Client_ClientDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Service Fabric client: Client_ClientDisconnected");

        }

        private void Client_ClientConnected(object sender, EventArgs e)
        {
            Console.WriteLine("Service Fabric client: Client_ClientConnected ");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            FabricClient client,
            Resolver resolver)
        {

            app.Map("/listservices", subApp =>
            {
                subApp.Run(async h =>
                {
                    var respons = await client.QueryManager.GetApplicationListAsync();
                    var services = await resolver.ListAvailableServices();
                    var jsonResponse = JsonConvert.SerializeObject(services, Formatting.Indented);
                    await h.Response.WriteAsync(jsonResponse);
                });
            });

            app.Use(async (context, next) =>
            {

                await context.Response.WriteAsync("Hello from Blanky Gateway.");
            });
        }
    }
}
