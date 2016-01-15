using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceRouter
{
    public static class Program
    {
        private const string ListeningAddress = "http://+:8283";

        public static void Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("ASPNET_ENV") == "Debug")
            {
                try
                {
                    // Build an ASP.NET 5 web application that serves as the communication listener.
                    var webApp = new WebApplicationBuilder().UseConfiguration(WebApplicationConfiguration.GetDefault())
                                                            .ConfigureLogging(factory =>
                                                            {
                                                                factory.AddConsole();
                                                            })
                                                            .UseStartup<Startup>()
                                                            .Build();

                    webApp.GetAddresses().Clear();
                    webApp.GetAddresses().Add(ListeningAddress);
                    Console.WriteLine(ListeningAddress);
                    webApp.Run();
                    Thread.Sleep(Timeout.Infinite);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Crashed: {0}", ex);
                    Console.ReadLine();
                }

            }

            using (var fabricRuntime = FabricRuntime.Create())
            {
                fabricRuntime.RegisterServiceType("ServiceRouterType", typeof(ServiceRouter));

                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}
