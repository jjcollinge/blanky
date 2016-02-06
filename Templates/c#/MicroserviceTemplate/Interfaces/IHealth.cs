using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroserviceTemplate
{
    public interface IHealth
    {
        Task<string> Check();
    }
}
