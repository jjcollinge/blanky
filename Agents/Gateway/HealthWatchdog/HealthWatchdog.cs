using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchdog
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class HealthWatchdog : StatelessService
    {
        public static HealthWatchdog CurrentInstance;
        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancelServiceInstance">Canceled when Service Fabric terminates this instance.</param>
        protected override async Task RunAsync(CancellationToken cancelServiceInstance)
        {
            //Set this for logging purposes. 
            CurrentInstance = this;
            HealthChecker checker = new HealthChecker(new ETWLogger());
            // This service instance continues processing until the instance is terminated.
            while (!cancelServiceInstance.IsCancellationRequested)
            {
                await checker.CheckHealthOfServices();

                // Pause for 1 second before continue processing.
                await Task.Delay(TimeSpan.FromSeconds(5), cancelServiceInstance);
            }
        }

    }
}
