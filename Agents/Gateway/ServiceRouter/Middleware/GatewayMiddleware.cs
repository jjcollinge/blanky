using Microsoft.AspNet.Http;
using Microsoft.AspNet.Proxy;
using ServiceRouter.ServiceDiscovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using System.Diagnostics;

namespace ServiceRouter.Middleware
{
    public class GatewayMiddleware
    {
        private readonly RequestDelegate next;
        private readonly Resolver resolver;
        public GatewayMiddleware(RequestDelegate next, Resolver resolver)
        {
            this.next = next;
            this.resolver = resolver;
        }

        public async Task Invoke(HttpContext context)
        {
            
            var intendedServiceEndpoint = await resolver.ResolveEndpoint(context);
            
            //Time how long the request spends in the gateway
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            //If the call is internal to the cluster assume endpoints are reachable 
            //so issue a redirect. This means no proxying so removes the gateway as a bottleneck. 
            if (context.Connection.IsLocal)
            {
                await Issue307RedirectToService(context, intendedServiceEndpoint);
            }
            else
            {
                //Stop timing when proxying as interested in gateway time not
                //how long the downstream service takes to respond. 
                stopwatch.Stop();

                await ProxyRequest(context, intendedServiceEndpoint);
            }

            stopwatch.Stop();
            context.Response.Headers.Add("x-blanky-gateway-time", stopwatch.ElapsedMilliseconds.ToString());

            await next.Invoke(context);
        }

        private static async Task Issue307RedirectToService(HttpContext context, string endpoint)
        {
            //Return a temporary redirect to the service endpoint

            context.Response.StatusCode = 307;
            context.Response.Headers.Add("Location", endpoint);
            await context.Response.WriteAsync($"Redirect issused as call originated locally. Endpoint: {endpoint}");
        }

        private async Task ProxyRequest(HttpContext context, string endpoint)
        {
            //proxy the requests through to the host
            var uri = new Uri(endpoint);
            var proxyMiddleware = new ProxyMiddleware(next, new ProxyOptions
            {
                Host = uri.Host,
                Port = uri.Port.ToString(),
                Scheme = uri.Scheme
            });
            await proxyMiddleware.Invoke(context);
        }
    }
}
