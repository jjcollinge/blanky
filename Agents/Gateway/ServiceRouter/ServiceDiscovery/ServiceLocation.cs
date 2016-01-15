using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Fabric.Query;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRouter.ServiceDiscovery
{
    public class ServiceLocation : IEquatable<ServiceLocation>
    {
        public ServiceLocation(HttpContext context)
        {
            ParseGatewayUrl(context.Request);
        }
        public ServiceLocation(Application app, Service service)
        {
            ApplicationTypeName = app.ApplicationTypeName;
            ServiceName = service.ServiceTypeName;

        }

        public string ApplicationTypeName { get; set; }
        public string ServiceName { get; set; }

        public Uri FabricAddress
        {
            get
            {
                return new Uri($"fabric:/{ApplicationTypeName}/{ServiceName}");
            }
        }

        private void ParseGatewayUrl(HttpRequest request)
        {
            var url = request.PathBase.Value + request.Path.Value;
            var pathComponents = request.Path.Value.Split('/');

            if (pathComponents.Length < 2)
            {
                throw new UnRoutableAddressException($"Address {url} doesn't match format fabric:/appname/servicename");
            }

            ApplicationTypeName = pathComponents[0];
            ServiceName = pathComponents[1];

            //Todo: Handle optional params like listenername and version. 

        }

        public bool Equals(ServiceLocation other)
        {
            if (other == null)
            {
                return false;
            }
            return FabricAddress == other.FabricAddress;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ServiceLocation);
        }

        public override int GetHashCode()
        {
            return FabricAddress.GetHashCode();
        }
    }
}
