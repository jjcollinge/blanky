﻿using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;

namespace Swashbuckle.Application
{
    public class SwaggerUiMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TemplateMatcher _requestMatcher;
        private readonly string _swaggerUrl;
        private readonly Assembly _resourceAssembly;

        public SwaggerUiMiddleware(
            RequestDelegate next,
            string baseRoute,
            string swaggerUrl
        )
        {
            _next = next;
            _requestMatcher = new TemplateMatcher(TemplateParser.Parse(baseRoute), new RouteValueDictionary());
            _swaggerUrl = swaggerUrl;
            _resourceAssembly = GetType().GetTypeInfo().Assembly;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!RequestingSwaggerUi(httpContext.Request))
            {
                await _next(httpContext);
                return;
            }

            var template = _resourceAssembly.GetManifestResourceStream("Swashbuckle.SwaggerUi.SwaggerUi.index.html");
            var content = AssignPlaceholderValuesTo(template);
            RespondWithContentHtml(httpContext.Response, content);
        }

        private bool RequestingSwaggerUi(HttpRequest request)
        {
            if (request.Method != "GET") return false;

            var routeValues = _requestMatcher.Match(request.Path);
            return (routeValues != null);
        }

        private Stream AssignPlaceholderValuesTo(Stream template)
        {
            var placeholderValues = new Dictionary<string, string>
            {
                { "%(SwaggerUrl)", _swaggerUrl }
            };

            var templateText = new StreamReader(template).ReadToEnd();
            var contentBuilder = new StringBuilder(templateText);
            foreach (var entry in placeholderValues)
            {
                contentBuilder.Replace(entry.Key, entry.Value);
            }

            return new MemoryStream(Encoding.UTF8.GetBytes(contentBuilder.ToString()));
        }

        private void RespondWithContentHtml(HttpResponse response, Stream content)
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            content.CopyTo(response.Body);
        }
    }
}
