using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Health;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HealthWatchdog
{
    public class HealthChecker
    {
        private readonly string GatewayEndpoint = "http://localhost:8283/list/endpoints";
        private readonly ILogger Logger;
        private static FabricClient Client = new FabricClient(new FabricClientSettings() { HealthReportSendInterval = TimeSpan.FromSeconds(0) });
        public HealthChecker(ILogger logger)
        {
            this.Logger = logger;
        }
        public HealthChecker(ILogger logger, string gatewayEndpoint)
        {
            this.Logger = logger;
            this.GatewayEndpoint = gatewayEndpoint;
        }

        public async Task CheckHealthOfServices()
        {
            // Log what the service is doing

            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(1);

            var services = await DiscoverServiceHttpEndpoints();
            foreach (var service in services)
            {
                using (BenchmarkStopwatch.Start($"CheckingService:{service.Key}", Logger))
                {
                    Logger.LogInformation($"Started checking {service.Key}");

                    foreach (var endpoint in service.Value)
                    {
                        await CheckEndpointAndReportHealthToCluster(httpClient, service, endpoint);
                    }

                    Logger.LogInformation($"Finished checking {service.Key}");
                }
            }

        }


        private async Task CheckEndpointAndReportHealthToCluster(HttpClient httpClient, KeyValuePair<string, string[]> service, string endpoint)
        {
            Logger.LogInformation($"Checking {service.Key} on endpoint {endpoint}");

            var healthEndpoint = $"{endpoint}/health";

            var httpResponse = await httpClient.GetAsync(healthEndpoint);

            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var responseBody = await httpResponse.Content.ReadAsStringAsync();
                Logger.LogInformation($"Error: {service.Key} on endpoint {endpoint} responded with '{responseBody}'");

                HealthState healthState = HealthState.Error;

                // Send report around the service health
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
                Logger.LogInformation($"OK: {service.Key} on endpoint {endpoint} responded with '{responseBody}'");
            }
        }


        private async Task<Dictionary<string, string[]>> DiscoverServiceHttpEndpoints()
        {
            HttpClient client = new HttpClient();
            var endpointApiResponse = await client.GetStringAsync(GatewayEndpoint);
            var json = JObject.Parse(endpointApiResponse);

            var servicesAndEndpoints =
                json.SelectToken("Results")
                .Children()
                .Select(x => new {
                    FabAddress = x.SelectToken("FabricAddress"),
                    Endpoints = x.SelectToken("AllInternalEndpoints")
                })
                //is it an http endpoint?
                .Where(x => x.Endpoints.Any(y => y.Value<string>().ToLower().Contains("http")))
                //map to dictionary (Todo: find better way of tolerantly working with json in c#)
                .ToDictionary(x => x.FabAddress.Value<string>(), value => value.Endpoints.Select(y => y.Value<string>()).ToArray());

            return servicesAndEndpoints;
        }


    }
}
