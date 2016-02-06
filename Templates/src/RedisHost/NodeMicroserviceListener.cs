using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeMicroservice
{
    class NodeMicroserviceListener : ICommunicationListener
    {
        public void Abort()
        {
            Process.GetProcessById(NodeMicroservice.servicePID).Kill();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Process.GetProcessById(NodeMicroservice.servicePID).Kill();
            return Task.FromResult(0);
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult($"{FabricRuntime.GetNodeContext().IPAddressOrFQDN}:30000");
        }
    }
}
