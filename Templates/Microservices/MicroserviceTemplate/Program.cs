using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MicroserviceTemplate
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var fabricRuntime = FabricRuntime.Create())
            {
                fabricRuntime.RegisterServiceType("MicroserviceTemplateType", typeof(MicroserviceTemplate));

                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}
