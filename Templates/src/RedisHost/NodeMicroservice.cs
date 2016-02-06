using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NodeMicroservice
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class NodeMicroservice : StatelessService
    {
        public static int servicePID { get; set; }
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            // TODO: If your service needs to handle user requests, return a list of ServiceReplicaListeners here.
            return new[] { new ServiceInstanceListener(_ => new NodeMicroserviceListener()) };
        }

        protected override async Task RunAsync(CancellationToken cancelServiceInstance)
        {

            Process process = Start();
            // This service instance continues processing until the instance is terminated.
            while (!cancelServiceInstance.IsCancellationRequested)
            {
                if (process.HasExited)
                {
                    process = Start();
                }
                process.WaitForExit(600);

            }

            process.Kill();
        }

        private static Process Start()
        {
            var currentDir = System.IO.Directory.GetCurrentDirectory();
            var exeDir = Path.Combine(currentDir, "node.exe");
            var configDir = Path.Combine(currentDir, "server.js");
            var process = System.Diagnostics.Process.Start(exeDir, configDir);
            servicePID = process.Id;
            return process;
        }
    }
}
