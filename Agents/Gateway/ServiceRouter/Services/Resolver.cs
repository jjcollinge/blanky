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

namespace ServiceRouter.Services
{

    public class Resolver : IDisposable
    {
        private const int SERVICE_LOCATION_CACHE_EXPIRY_SECONDS = 30;
        private const int CACHE_REFRESH_TIME_SECONDS = 20;
        private const string DEFAULT_LISTENER_NAME = "HttpsEndpoint";

        private readonly SimpleEndpointResolverClientFactory EndpointResolver = new SimpleEndpointResolverClientFactory();
        private readonly MemoryCache ServiceCache = new MemoryCache(new MemoryCacheOptions());
        private readonly MemoryCacheEntryOptions MemoryCacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(SERVICE_LOCATION_CACHE_EXPIRY_SECONDS)
        };

        private FabricClient fabClient { get; set; }
        private ILogger logger { get; set; }
        private System.Timers.Timer cacheUpdateTimer { get; set; }

        public Resolver(FabricClient fabClient, ILogger logger)
        {
            this.fabClient = fabClient;
            this.logger = logger;
            DiscoverClusterServices();

            cacheUpdateTimer = new System.Timers.Timer(TimeSpan.FromSeconds(CACHE_REFRESH_TIME_SECONDS).TotalMilliseconds);
            cacheUpdateTimer.Elapsed += CacheUpdateTimer_Elapsed;
        }

        private async void CacheUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                await DiscoverClusterServices();
            }
            catch (Exception ex)
            {
                logger.LogError("Resolver failed to update memory cache with services", ex);   
            }

        }

        private async Task DiscoverClusterServices()
        {
            var applications = await fabClient.QueryManager.GetApplicationListAsync();
            foreach (var application in applications)
            {
                foreach (var service in await fabClient.QueryManager.GetServiceListAsync(application.ApplicationName))
                {
                    var serviceLocation = new ServiceLocation(application, service);

                    ServiceCache.Set(
                        serviceLocation.FabricAddress,
                        serviceLocation,
                        MemoryCacheEntryOptions);
                }
            }
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
