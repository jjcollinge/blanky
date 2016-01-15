using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceRouter.Services
{
    public class SimpleEndpointResolverClient : ICommunicationClient
    {
        public SimpleEndpointResolverClient()
        {
        }
        public string Endpoint { get; set; }
        public bool IsValid { get; set; }
        public ResolvedServicePartition ResolvedServicePartition { get; set; }
    }

    public class SimpleEndpointResolverClientFactory : CommunicationClientFactoryBase<SimpleEndpointResolverClient>
    {
        protected override void AbortClient(SimpleEndpointResolverClient client)
        {
            client.IsValid = false;
        }

        protected override Task<SimpleEndpointResolverClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {

            var simpleClient = new SimpleEndpointResolverClient
            {
                Endpoint = endpoint,
                IsValid = true
            };
            return Task.FromResult<SimpleEndpointResolverClient>(simpleClient);
        }

        protected override bool ValidateClient(SimpleEndpointResolverClient clientChannel)
        {
            //No persistent channel established as general purpose so nothing to validate. 
            return true;
        }

        protected override bool ValidateClient(string endpoint, SimpleEndpointResolverClient client)
        {
            return endpoint == client.Endpoint;
        }
    }
}
