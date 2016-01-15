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
        private const string DEFAULT_LISTENER_NAME = "HttpsEndpoint";

        //private readonly SimpleEndpointResolverClientFactory EndpointResolver = new SimpleEndpointResolverClientFactory();
        private readonly SimpleEndpointResolverClientFactory EndpointResolver;
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
            DiscoverClusterServices();

            cacheUpdateTimer = new System.Timers.Timer(TimeSpan.FromSeconds(CACHE_REFRESH_TIME_SECONDS).TotalMilliseconds);
            cacheUpdateTimer.Elapsed += CacheUpdateTimer_Elapsed;
        }

        private async void CacheUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
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
            foreach(var service in services)
            {
                ServiceCache.Set(service.FabricAddress, service);
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

        public async Task<string> ResolveEndpoint(HttpContext request)
        {
            var targetServiceLocation = new ServiceLocation(request);

            var simpleClient = await EndpointResolver.GetClientAsync(
                targetServiceLocation.FabricAddress,
                DEFAULT_LISTENER_NAME,
                CancellationToken.None);

            var cacheServiceEntry = ServiceCache.Get<ServiceLocation>(targetServiceLocation.FabricAddress);

            if (cacheServiceEntry == null)
            {
                throw new FabricServiceNotFoundException($"Service: {targetServiceLocation.FabricAddress} isn't available in the cluster");
            }

            return simpleClient.Endpoint;
        }

        public void Dispose()
        {
            ServiceCache.Dispose();
            cacheUpdateTimer.Dispose();
        }
    }

    
}
