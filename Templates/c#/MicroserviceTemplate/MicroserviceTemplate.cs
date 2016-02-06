using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.ServiceFabric.AspNet;

namespace MicroserviceTemplate
{
    public class MicroserviceTemplate : StatelessService
    {
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            // Build an ASP.NET 5 web application that serves as the communication listener.
            var webApp = new WebApplicationBuilder().UseConfiguration(WebApplicationConfiguration.GetDefault())
                                                    .UseStartup<Startup>()
                                                    .Build();

            // Replace the address with the one dynamically allocated by Service Fabric.
            string listeningAddress = AspNetCommunicationListener.GetListeningAddress(ServiceInitializationParameters, "MicroserviceTemplateType");
            webApp.GetAddresses().Clear();
            webApp.GetAddresses().Add(listeningAddress);

            return new[] { new ServiceInstanceListener(_ => new AspNetCommunicationListener(webApp)) };
        }
    }
}
