using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Http.FileServer
{
    public class HttpBasedApplicationWrapper : IHttpApplication<IFeatureCollection>
    {
        private readonly IQuasiHttpApplication _wrapped;

        public HttpBasedApplicationWrapper(IQuasiHttpApplication wrapped)
        {
            _wrapped = wrapped;
        }

        public IFeatureCollection CreateContext(IFeatureCollection contextFeatures)
        {
            return contextFeatures;
        }

        public void DisposeContext(IFeatureCollection contextFeatures, Exception exception)
        {
        }

        public async Task ProcessRequestAsync(IFeatureCollection contextFeatures)
        {
            HttpContext httpContext = new DefaultHttpContext(contextFeatures);
            var quasiRequest = new DefaultQuasiHttpRequest
            {
                HttpMethod = httpContext.Request.Method,
                HttpVersion = httpContext.Request.Protocol
            };
            quasiRequest.Path = ReconstructRequestPath(httpContext.Request);
            quasiRequest.Headers = ReconstructRequestHeaders(httpContext.Request.Headers);
            if (httpContext.Request.Body != null)
            {
                quasiRequest.Body = new StreamBackedBody(httpContext.Request.Body,
                    httpContext.Request.ContentType);
            }
            var quasiResponse = await _wrapped.ProcessRequest(quasiRequest,
                new Dictionary<string, object>());
            SetResponseStatusAndHeaders(quasiResponse, httpContext.Response);
            if (quasiResponse.Body != null)
            {
                var data = new byte[TransportUtils.DefaultMaxChunkSize];
                while (true)
                {
                    var bytesRead = await quasiResponse.Body.ReadBytes(data, 0, data.Length);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    await httpContext.Response.Body.WriteAsync(data, 0, bytesRead);
                }
            }
        }

        private static string ReconstructRequestPath(HttpRequest request)
        {
            var quasiHttpRequestPath = new StringBuilder();
            quasiHttpRequestPath.Append(request.PathBase);
            quasiHttpRequestPath.Append(request.Path);
            if (request.QueryString.HasValue)
            {
                quasiHttpRequestPath.Append(request.QueryString);
            }
            return quasiHttpRequestPath.ToString();
        }

        private IDictionary<string, List<string>> ReconstructRequestHeaders(IHeaderDictionary headers)
        {
            var quasiHttpRequestHeaders = new Dictionary<string, List<string>>();
            foreach (var entry in headers)
            {
                quasiHttpRequestHeaders.Add(entry.Key, entry.Value.ToList());
            }
            return quasiHttpRequestHeaders;
        }

        private void SetResponseStatusAndHeaders(IQuasiHttpResponse quasiHttpResponse,
            HttpResponse response)
        {
            response.StatusCode = quasiHttpResponse.StatusIndicatesSuccess ?
                200 : (quasiHttpResponse.StatusIndicatesClientError ? 400 : 500);
            if (quasiHttpResponse.HttpStatusCode > 0)
            {
                response.StatusCode = quasiHttpResponse.HttpStatusCode;
            }
            if (quasiHttpResponse.Headers != null)
            {
                foreach (var entry in quasiHttpResponse.Headers)
                {
                    response.Headers.Add(entry.Key, new StringValues(entry.Value.ToArray()));
                }
            }
            if (quasiHttpResponse.Body != null)
            {
                if (quasiHttpResponse.Body.ContentLength >= 0)
                {
                    response.Headers.Add("Content-Length",
                        new StringValues(quasiHttpResponse.Body.ContentLength.ToString()));
                }
                else
                {
                    response.Headers.Add("Transfer-Encoding", new StringValues("chunked"));
                }
            }
            if (quasiHttpResponse.Body != null && quasiHttpResponse.Body.ContentType != null)
            {
                response.Headers.Add("Content-Type",
                    new StringValues(quasiHttpResponse.Body.ContentType));
            }
        }
    }
}
