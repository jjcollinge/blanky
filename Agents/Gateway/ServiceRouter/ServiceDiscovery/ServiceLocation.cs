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
            ApplicationName = app.ApplicationName.ToString().Replace("fabric:/", "");
            ServiceTypeName = service.ServiceTypeName;
            ServiceName = service.ServiceName.ToString().Replace($"fabric:/{ApplicationName}/", "");
            ServiceVersion = service.ServiceManifestVersion;
            IsStatefulService = service.ServiceKind == ServiceKind.Stateful;
        }

        public bool IsStatefulService { get; set; }
        public string ApplicationTypeName { get; set; }
        public string ApplicationName { get; set; }
        public string ServiceTypeName { get; set; }
        public string ServiceName { get; set; }
        public string ServiceVersion { get; set; }

        public Uri FabricAddress
        {
            get
            {
                return new Uri($"fabric:/{ApplicationName}/{ServiceName}");
            }
        }

        private void ParseGatewayUrl(HttpRequest request)
        {
            var url = request.PathBase.Value + request.Path.Value;
            var pathComponents = url.TrimStart('/').Split('/');

            if (pathComponents.Length < 2 || pathComponents[0] != "route")
            {
                throw new UnRoutableAddressException($"Address {url} doesn't match format http://localhost/route/appname/servicename");
            }

            ApplicationName = pathComponents[1];
            ServiceName = pathComponents[2];

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
