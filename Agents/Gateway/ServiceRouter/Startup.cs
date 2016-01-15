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
            Resolver resolver)
        {
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
                    if (!context.Connection.IsLocal)
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
                    //proxy the requests through to the host
                    //Todo: consider caching Proxymiddlewares by host:port to reduce creation of multiple. 
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
                    
BBBBBBBBBBBBBBBBB   lllllll                                    kkkkkkkk                                    
B::::::::::::::::B  l:::::l                                    k::::::k                                    
B::::::BBBBBB:::::B l:::::l                                    k::::::k                                    
BB:::::B     B:::::Bl:::::l                                    k::::::k                                    
  B::::B     B:::::B l::::l   aaaaaaaaaaaaa  nnnn  nnnnnnnn     k:::::k    kkkkkkkyyyyyyy           yyyyyyy
  B::::B     B:::::B l::::l   a::::::::::::a n:::nn::::::::nn   k:::::k   k:::::k  y:::::y         y:::::y 
  B::::BBBBBB:::::B  l::::l   aaaaaaaaa:::::an::::::::::::::nn  k:::::k  k:::::k    y:::::y       y:::::y  
  B:::::::::::::BB   l::::l            a::::ann:::::::::::::::n k:::::k k:::::k      y:::::y     y:::::y   
  B::::BBBBBB:::::B  l::::l     aaaaaaa:::::a  n:::::nnnn:::::n k::::::k:::::k        y:::::y   y:::::y    
  B::::B     B:::::B l::::l   aa::::::::::::a  n::::n    n::::n k:::::::::::k          y:::::y y:::::y     
  B::::B     B:::::B l::::l  a::::aaaa::::::a  n::::n    n::::n k:::::::::::k           y:::::y:::::y      
  B::::B     B:::::B l::::l a::::a    a:::::a  n::::n    n::::n k::::::k:::::k           y:::::::::y       
BB:::::BBBBBB::::::Bl::::::la::::a    a:::::a  n::::n    n::::nk::::::k k:::::k           y:::::::y        
B:::::::::::::::::B l::::::la:::::aaaa::::::a  n::::n    n::::nk::::::k  k:::::k           y:::::y         
B::::::::::::::::B  l::::::l a::::::::::aa:::a n::::n    n::::nk::::::k   k:::::k         y:::::y          
BBBBBBBBBBBBBBBBB   llllllll  aaaaaaaaaa  aaaa nnnnnn    nnnnnnkkkkkkkk    kkkkkkk       y:::::y           
                                                                                        y:::::y            
                                                                                       y:::::y             
                                                                                      y:::::y              
                                                                                     y:::::y               
                                                                                    yyyyyyy                
                                                                                                                                                                                                            

  __  __       _    _                _____                 _            ______    _          _      
 |  \/  |     | |  (_)              / ____|               (_)          |  ____|  | |        (_)     
 | \  / | __ _| | ___ _ __   __ _  | (___   ___ _ ____   ___  ___ ___  | |__ __ _| |__  _ __ _  ___ 
 | |\/| |/ _` | |/ / | '_ \ / _` |  \___ \ / _ \ '__\ \ / / |/ __/ _ \ |  __/ _` | '_ \| '__| |/ __|
 | |  | | (_| |   <| | | | | (_| |  ____) |  __/ |   \ V /| | (_|  __/ | | | (_| | |_) | |  | | (__ 
 |_|  |_|\__,_|_|\_\_|_| |_|\__, | |_____/ \___|_|    \_/ |_|\___\___| |_|_ \__,_|_.__/|_|  |_|\___|
                             __/ |              | |                | |   | | |                      
 __      ____ _ _ __ _ __ __|___/ __ _ _ __   __| |   ___ _   _  __| | __| | |_   _                 
 \ \ /\ / / _` | '__| '_ ` _ \   / _` | '_ \ / _` |  / __| | | |/ _` |/ _` | | | | |                
  \ V  V / (_| | |  | | | | | | | (_| | | | | (_| | | (__| |_| | (_| | (_| | | |_| |                
   \_/\_/ \__,_|_|  |_| |_| |_|  \__,_|_| |_|\__,_|  \___|\__,_|\__,_|\__,_|_|\__, |                
                                                                               __/ |                
                                                                              |___/                 
                    Help
                    ---------------     
    
                    View Operations:
                        - list/services
                        - list/endpoints
                    
                    N.B. To access a service endpoints externally from the cluster use 'RoutedEndpoint'
                         replace 'localhost:8283' with the External IP/Port and ensure
                         port forwarding is correctly configured on the load balancer for the service router. 

                    Route Operations:
                        - route/{ApplicationName}/{ServiceName}/{HttpMethod}
                    
                    N.B. Cluster routing reqeusts originating in a SF cluster will receive Redirect:307 
                         with the actual service endpoint to avoid bottle neck or throughput issues in the ServiceRouter
                    ");
            });
        }
    }
}
