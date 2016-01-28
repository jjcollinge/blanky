using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.AspNet.Http;
using System.Fabric.Query;
using System.Timers;
using Microsoft.Extensions.Logging;
using System.Threading;
using Newtonsoft.Json;

namespace ServiceRouter.ServiceDiscovery
{
    /// <summary>
    /// The resolver handles requests to the gateway
    /// mapping these to internal service fabric services
    /// accross the cluster and returning an http endpoint 
    /// which traffic can route too. 
    /// 
    /// Updates to the available services happen on a timer. 
    /// </summary>
    public class Resolver : IDisposable
    {
        private const int SERVICE_LOCATION_CACHE_EXPIRY_SECONDS = 30;
        private const int CACHE_REFRESH_TIME_SECONDS = 20;
        private const string DEFAULT_LISTENER_NAME = "";

        private readonly SimpleEndpointResolverClientFactory EndpointResolver = new SimpleEndpointResolverClientFactory();
        private readonly MemoryCache ServiceCache = new MemoryCache(new MemoryCacheOptions());
        private readonly MemoryCacheEntryOptions MemoryCacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(SERVICE_LOCATION_CACHE_EXPIRY_SECONDS)
        };

        private FabricClient fabClient { get; set; }
        private ILogger logger { get; set; }
        private System.Timers.Timer cacheUpdateTimer { get; set; }


        public Resolver(FabricClient fabClient, ILoggerFactory loggerFactory)
        {
            this.fabClient = fabClient;
            this.logger = loggerFactory.CreateLogger("Service Resolver");

            PopulateServiceCache();

            cacheUpdateTimer = new System.Timers.Timer(TimeSpan.FromSeconds(CACHE_REFRESH_TIME_SECONDS).TotalMilliseconds);
            cacheUpdateTimer.Elapsed += CacheUpdateTimer_UpdateServiceCache;
        }

        private void PopulateServiceCache()
        {
            DiscoverClusterServices().ContinueWith(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    AddServicesToCache(task.Result);
                }
            });
        }

        private async void CacheUpdateTimer_UpdateServiceCache(object sender, ElapsedEventArgs e)
        {
            try
            {
                logger.LogInformation("Started: Discovering services in cluster and adding to cache");

                var services = await DiscoverClusterServices();
                AddServicesToCache(services);

                logger.LogInformation("Finished: Discovering services in cluster and adding to cache");

            }
            catch (Exception ex)
            {
                logger.LogError("Resolver failed to update memory cache with services", ex);
            }

        }

        private void AddServicesToCache(List<ServiceLocation> services)
        {
            foreach (var service in services)
            {
                ServiceCache.Set(service.FabricAddress.ToString(), service);
            }


        }

        private async Task<List<ServiceLocation>> DiscoverClusterServices()
        {
            var services = new List<ServiceLocation>();
            var applications = await fabClient.QueryManager.GetApplicationListAsync();
            foreach (var application in applications)
            {
                foreach (var service in await fabClient.QueryManager.GetServiceListAsync(application.ApplicationName))
                {
                    services.Add(new ServiceLocation(application, service));

                }
            }
            return services;
        }

        public async Task<List<ServiceLocation>> ListAvailableServices()
        {
            return await DiscoverClusterServices();
        }

        public async Task<List<EndpointResponseModel>> ListServiceEndpoints()
        {
            var servicesWithEndpoints = new List<EndpointResponseModel>();
            foreach (var service in await DiscoverClusterServices())
            {
                EndpointResponseModel endpointResult;
                if (service.IsStatefulService)
                {
                    endpointResult = new EndpointResponseModel
                    {
                        FabricAddress = service.FabricAddress.ToString(),
                        IsSuccess = false,
                        ErrorDetails = "StatefulService - Need Partition To Determine Endpoint"
                    };
                }
                else
                {
                    try
                    {
                        endpointResult = await GetEndpointModel(service.FabricAddress.ToString());
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Failed to get endpoint for stateless service", ex);
                        endpointResult = new EndpointResponseModel
                        {
                            IsSuccess = false,
                            ErrorDetails = ex.Message
                        };
                    }
                }

                servicesWithEndpoints.Add(endpointResult);
            }

            return servicesWithEndpoints;
        }

        private async Task<EndpointResponseModel> GetEndpointModel(string fabricAddress)
        {
            var endpoints = await GetInstanceEndpoints(fabricAddress);
            var endpointResult = new EndpointResponseModel
            {
                FabricAddress = fabricAddress,
                IsSuccess = true,
                AllInternalEndpoints = endpoints,
                InternalEndpointRandom = endpoints.OrderBy(x => Guid.NewGuid()).First(), //Todo: optimize this, endure even distribution
                IsRoutableByGateway = endpoints.Any(x => x.ToLower().Contains("http")),
                //Todo: get the port from config. 
                RoutedEndpoint = fabricAddress.ToString().Replace("fabric:/", "http://localhost:8505/route/")
            };
            return endpointResult;
        }

        public async Task<string> ResolveEndpoint(HttpContext request)
        {
            //Parse the request to get service location
            var targetServiceLocation = new ServiceLocation(request);

            ThrowIfServiceNotPresent(targetServiceLocation.FabricAddress.ToString());

            var possibleEndpoints = await GetEndpointModel(targetServiceLocation.FabricAddress.ToString());

            return possibleEndpoints.InternalEndpointRandom;
        }

        public async Task<string> ResolveEndpoint(string fabricAddress)
        {

            ThrowIfServiceNotPresent(fabricAddress);

            var possibleEndpoints = await GetEndpointModel(fabricAddress);

            return possibleEndpoints.InternalEndpointRandom;
        }

        private void ThrowIfServiceNotPresent(string fabricAddress)
        {
            var cacheServiceEntry = ServiceCache.Get<ServiceLocation>(fabricAddress);
            if (cacheServiceEntry == null)
            {
                throw new FabricServiceNotFoundException($"Service: {fabricAddress} isn't available in the cluster");
            }
        }

        private async Task<string[]> GetInstanceEndpoints(string fabricAddress)
        {

            //Get the endpoint for the service
            var serviceEndpoint = await fabClient.ServiceManager.ResolveServicePartitionAsync(new Uri(fabricAddress));
            
            var addressesDeserialized = serviceEndpoint.Endpoints.Select(x => JsonConvert.DeserializeObject<EndpointServiceFabricModel>(x.Address));
            var simpleEndpoints = addressesDeserialized.SelectMany(x => x.Endpoints.Values);
            return simpleEndpoints.ToArray();
        }

        //private async Task<SimpleEndpointResolverClient> GetEndpointFromServiceLocation(ServiceLocation targetServiceLocation)
        //{
        //    return await EndpointResolver.GetClientAsync(
        //        targetServiceLocation.FabricAddress,
        //        DEFAULT_LISTENER_NAME,
        //        CancellationToken.None);
        //}

        public void Dispose()
        {
            ServiceCache.Dispose();
            cacheUpdateTimer.Dispose();
        }
    }


}
