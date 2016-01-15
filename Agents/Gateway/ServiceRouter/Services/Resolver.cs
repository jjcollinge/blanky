using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace ServiceRouter.Services
{
    public class Resolver
    {
        private static MemoryCache EndpointCache = new MemoryCache(new MemoryCacheOptions());

        private FabricClient fabClient;

        public Resolver(FabricClient fabClient)
        {
            this.fabClient = fabClient;
        }


    }
}
