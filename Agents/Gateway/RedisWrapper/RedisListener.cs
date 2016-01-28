using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Fabric;
using System.Diagnostics;

namespace RedisWrapper
{
    class RedisListener : ICommunicationListener
    {
        

        public void Abort()
        {
            Process.GetProcessById(RedisWrapper.RedisPID).Kill();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Process.GetProcessById(RedisWrapper.RedisPID).Kill();
            return Task.FromResult(0);
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {

            return Task.FromResult($"{FabricRuntime.GetNodeContext().IPAddressOrFQDN}:6379");
        }
    }
}
