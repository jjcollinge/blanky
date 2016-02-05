using Microsoft.AspNet.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceDeployer
{
    public class ServiceDeployer : StatelessService
    {

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            // Build an ASP.NET 5 web application that serves as the communication listener.
            var webApp = new WebApplicationBuilder().UseConfiguration(WebApplicationConfiguration.GetDefault())
                                                    .UseStartup<Startup>()
                                                    .Build();

            // Replace the address with the one dynamically allocated by Service Fabric.
            string listeningAddress = AspNetCommunicationListener.GetListeningAddress(ServiceInitializationParameters, "ServiceRouterTypeEndpoint");
            webApp.GetAddresses().Clear();
            webApp.GetAddresses().Add(listeningAddress);

            return new[] { new ServiceInstanceListener(_ => new AspNetCommunicationListener(webApp)) };
        }

        //protected override async Task RunAsync(CancellationToken cancellationToken)
        //{


        //    await base.RunAsync(cancellationToken);
        //}

        //private static async Task<string> DiscoverService(string fabricAddress)
        //{
        //    HttpClient client = new HttpClient();
        //    var endpointApiResponse = await client.GetStringAsync("http://localhost:8283/list/endpoints");
        //    var deserializedApiResponse = JsonConvert.DeserializeObject<Dictionary<String, EndpointResponseModel>>(endpointApiResponse);
        //    var redisEndpoint = deserializedApiResponse[fabricAddress].InternalEndpointRandom;
        //    return redisEndpoint;
        //}
    }
}
