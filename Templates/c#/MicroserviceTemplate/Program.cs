using Microsoft.AspNet.Hosting;
using Microsoft.ServiceFabric.AspNet;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace MicroserviceTemplate
{
    public static class Program
    {
        private const string ListeningAddress = "http://+:8283";

        public static void Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("ASPNET_ENV") == "Debug")
            {
                // Build an ASP.NET 5 web application that serves as the communication listener.
                var webApp = new WebApplicationBuilder().UseConfiguration(WebApplicationConfiguration.GetDefault())
                                                        .ConfigureLogging(factory =>
                                                        {
                                                            factory.AddConsole();
                                                        })
                                                        .UseStartup<Startup>()
                                                        .Build();

                // Replace the address with the one dynamically allocated by Service Fabric.
                webApp.GetAddresses().Clear();
                webApp.GetAddresses().Add(ListeningAddress);
                webApp.Run();

                Thread.Sleep(Timeout.Infinite);

            }
            else
            {
                using (var fabricRuntime = FabricRuntime.Create())
                {
                    fabricRuntime.RegisterServiceType("MicroserviceTemplateType", typeof(MicroserviceTemplate));

                    Thread.Sleep(Timeout.Infinite);
                }
            }
        }
    }
}
