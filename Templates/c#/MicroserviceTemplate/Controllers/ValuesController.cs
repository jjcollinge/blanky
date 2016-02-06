using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroserviceTemplate.Controllers
{
    [Route("/api")]
    [Produces("application/json")]
    public class ValuesController : Controller
    {
        [HttpGet]
        public string Get()
        {
            return "This is an example service controller";
        }
    }
}
