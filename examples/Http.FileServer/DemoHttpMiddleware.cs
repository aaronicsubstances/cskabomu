using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Server;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using NLog;

namespace Http.FileServer
{
    public class DemoHttpMiddleware : IHttpApplication<IFeatureCollection>
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly StandardQuasiHttpServer _wrapped;

        public DemoHttpMiddleware(StandardQuasiHttpServer wrapped)
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
                Method = httpContext.Request.Method,
                HttpVersion = httpContext.Request.Protocol,
                Environment = null, // can later send in some information about remote and local endpoints
            };
            quasiRequest.Target = ReconstructRequestPath(httpContext.Request);
            quasiRequest.Headers = ReconstructRequestHeaders(httpContext.Request.Headers);
            if (httpContext.Request.Body != null)
            {
                LOG.Debug("incoming file = {0}; transfer-enconding = {1}; content-length = {2}; content-type = {3} ...",
                    httpContext.Request.Headers["f"],
                    httpContext.Request.Headers["Transfer-Encoding"],
                    httpContext.Request.ContentLength,
                    httpContext.Request.ContentType);
                quasiRequest.Body = new CustomReaderBackedBody(
                    new StreamCustomReaderWriter(httpContext.Request.Body))
                {
                    ContentLength = httpContext.Request.ContentLength ?? -1
                };
            }
            var processingOptions = new DefaultQuasiHttpProcessingOptions
            {
                TimeoutMillis = 5_000,
                MaxChunkSize = 2 * 8192
            };
            IQuasiHttpResponse quasiResponse;
            try
            {
                quasiResponse = await _wrapped.AcceptRequest(quasiRequest,
                    processingOptions);
            }
            catch (Exception e)
            {
                LOG.Error(e, "http request processing failed");
                quasiResponse = new DefaultQuasiHttpResponse
                {
                    Body = new StringBody(e.Message)
                };
            }
            SetResponseStatusAndHeaders(quasiResponse, httpContext.Response);
            if (quasiResponse.Body != null)
            {
                var reader = quasiResponse.Body.AsReader();
                await IOUtils.CopyBytes(reader,
                    new StreamCustomReaderWriter(httpContext.Response.Body));
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

        private IDictionary<string, IList<string>> ReconstructRequestHeaders(IHeaderDictionary headers)
        {
            var quasiHttpRequestHeaders = new Dictionary<string, IList<string>>();
            foreach (var entry in headers)
            {
                quasiHttpRequestHeaders.Add(entry.Key, entry.Value.ToList());
            }
            return quasiHttpRequestHeaders;
        }

        private void SetResponseStatusAndHeaders(IQuasiHttpResponse quasiHttpResponse,
            HttpResponse response)
        {
            response.StatusCode = quasiHttpResponse.StatusCode;
            if (quasiHttpResponse.Headers != null)
            {
                foreach (var entry in quasiHttpResponse.Headers)
                {
                    // skip key headers.
                    switch (entry.Key.ToLower())
                    {
                        case "transfer-encoding":
                            break;
                        case "content-length":
                            break;
                        default:
                            response.Headers.Add(entry.Key, new StringValues(entry.Value.ToArray()));
                            break;
                    }
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
        }
    }
}
