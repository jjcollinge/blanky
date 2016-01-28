using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Health;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchdog
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class HealthWatchdog : StatelessService
    {

        private static FabricClient Client = new FabricClient(new FabricClientSettings() { HealthReportSendInterval = TimeSpan.FromSeconds(0) });
        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancelServiceInstance">Canceled when Service Fabric terminates this instance.</param>
        protected override async Task RunAsync(CancellationToken cancelServiceInstance)
        {
            // This service instance continues processing until the instance is terminated.
            while (!cancelServiceInstance.IsCancellationRequested)
            {

                // Log what the service is doing
                ServiceEventSource.Current.ServiceMessage(this, "");

                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(1);

                var services = await DiscoverServiceHttpEndpoints();
                foreach (var service in services)
                {
                    ServiceEventSource.Current.ServiceMessage(this, $"Started checking {service.Key}");

                    foreach (var endpoint in service.Value)
                    {
                        ServiceEventSource.Current.ServiceMessage(this, $"Checking {service.Key} on endpoint {endpoint}");

                        var healthEndpoint = $"{endpoint}/health";

                        var httpResponse = await httpClient.GetAsync(healthEndpoint);

                        if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            var responseBody = await httpResponse.Content.ReadAsStringAsync();
                            ServiceEventSource.Current.ServiceMessage(this, $"Error: {service.Key} on endpoint {endpoint} responded with '{responseBody}'");

                            HealthState healthState = HealthState.Error;

                            // Send report on deployed service package, as the connectivity is needed by the specific service manifest
                            // and can be different on different nodes
                            var serviceHealthReport = new ServiceHealthReport(
                                new Uri(service.Key),
                                new HealthInformation(
                                    "Blanky-HealthWatchdog",
                                    $"Healthendpoint didn't return 200ok on endpoint {endpoint}",
                                    healthState));

                            Client.HealthManager.ReportHealth(serviceHealthReport);
                        }
                        else
                        {
                            var responseBody = await httpResponse.Content.ReadAsStringAsync();
                            ServiceEventSource.Current.ServiceMessage(this, $"OK: {service.Key} on endpoint {endpoint} responded with '{responseBody}'");
                        }
                    }

                    
                }

                // Pause for 1 second before continue processing.
                await Task.Delay(TimeSpan.FromSeconds(5), cancelServiceInstance);
            }
        }



        private static async Task<Dictionary<string, string[]>> DiscoverServiceHttpEndpoints()
        {
            HttpClient client = new HttpClient();
            var endpointApiResponse = await client.GetStringAsync("http://localhost:8283/list/endpoints");
            var json = JObject.Parse(endpointApiResponse);

            var servicesAndEndpoints = 
                json.SelectToken("Results")
                .Children()
                .Select(x => new {
                    FabAddress = x.SelectToken("FabricAddress"),
                    Endpoints = x.SelectToken("AllInternalEndpoints")
                })
                //is it an http endpoint?
                .Where(x=>x.Endpoints.Any(y=>y.Value<string>().ToLower().Contains("http")))
                //map to dictionary (Todo: find better way of tolerantly working with json in c#)
                .ToDictionary(x => x.FabAddress.Value<string>(), value => value.Endpoints.Select(y => y.Value<string>()).ToArray()); 

            return servicesAndEndpoints;
        }
    }
}
