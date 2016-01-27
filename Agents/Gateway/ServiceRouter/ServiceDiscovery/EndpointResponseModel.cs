using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRouter.ServiceDiscovery
{
    public class EndpointResponseModel
    {
        public string RoutedEndpoint { get; set; }
        public string InternalEndpoint { get; set; }
        public bool IsSuccess { get; set; }
        public string Details { get; set; }
        public bool IsRoutableByGateway { get; set; }
    }
}
