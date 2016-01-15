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
using Microsoft.AspNet.Proxy;

namespace ServiceRouter
{
    public class Startup
    {
        private const string SESSION_KEY_SERVICE_ENDPOINT = "resolvedEndpoint";

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
            //services.AddCaching();
            //services.AddSession();
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
            //services.AddCaching();
            //services.AddSession();
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
            Resolver resolver)
        {
            ////Enable session, used to share items between middleware. 
            ////Todo: review to see if this can be done more effectively using the context object. 

            //app.UseSession();

            app.Map("/list/services", subApp =>
            {
                subApp.Run(async h =>
                {
                    var services = await resolver.ListAvailableServices();
                    var jsonResponse = JsonConvert.SerializeObject(services, Formatting.Indented);
                    await h.Response.WriteAsync(jsonResponse);
                });
            });

            app.Map("/list/endpoints", subApp =>
            {
                subApp.Run(async h =>
                {
                    var services = await resolver.ListServiceEndpoints();
                    var jsonResponse = JsonConvert.SerializeObject(services, Formatting.Indented);
                    await h.Response.WriteAsync(jsonResponse);
                });
            });

            
            app.Map("/route", subApp =>
            {
                subApp.Use(async (context, next) =>
                {
                    var endpoint = await resolver.ResolveEndpoint(context);
                    
                    context.Items.Add(SESSION_KEY_SERVICE_ENDPOINT, endpoint);

                    await next.Invoke();
                });
                subApp.Use(async (context, next) =>
                {
                    //Move on if it's not a local connection as this means it's come from outside 
                    //the cluster and 307 redirect wouldn't work
                    if (context.Connection.IsLocal)
                    {
                        await next.Invoke();
                    }
                    else
                    {
                        var endpoint = context.Items[SESSION_KEY_SERVICE_ENDPOINT].ToString();

                        //Return a temporary redirect to the service endpoint
                        context.Response.StatusCode = 307;
                        context.Response.Headers.Add("Location", endpoint);
                        
                    }

                });

                subApp.Use(async (context, next) =>
                {
                    var endpoint = context.Items[SESSION_KEY_SERVICE_ENDPOINT].ToString();
                    var uri = new Uri(endpoint);
                    var proxyMiddleware = new ProxyMiddleware(r => next.Invoke(), new ProxyOptions
                    {
                        Host = uri.Host,
                        Port = uri.Port.ToString(),
                        Scheme = uri.Scheme
                    });
                    await proxyMiddleware.Invoke(context);
                });
            });

            app.Use(async (context, next) =>
            {

                await context.Response.WriteAsync(@"
                    View Operations:
                        - list/services
                        - list/endpoints
                    Route Operations:
                        - route/{ApplicationName}/{ServiceName}/{HttpMethod}
                    
                    N.B. Cluster routing reqeusts originating in a SF cluster will receive Redirect:307 
                         with the actual service endpoint to avoid bottle neck or throughput issues in the ServiceRouter
                    ");
            });
        }
    }
}
