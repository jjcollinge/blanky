using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroserviceTemplate
{
    public class MyHealth : IHealth
    {
        public Task<string> Check()
        {
            return Task.FromResult("Service is good");
        }
    }
}
