using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRouter.ServiceDiscovery
{
    public class EndpointResponseModel
    {
        public string FabricAddress { get; set; }
        public bool IsSuccess { get; set; }
        public string InternalEndpointRandom { get; set; }
        public string[] AllInternalEndpoints { get; set; }
        public bool IsRoutableByGateway { get; set; }
        public string RoutedEndpoint { get; set; }
        public string ErrorDetails { get; set; }
    }
}
