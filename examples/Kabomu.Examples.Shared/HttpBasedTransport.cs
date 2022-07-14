using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class HttpBasedTransport : IQuasiHttpTransportBypass
    {
        private readonly HttpClient _httpClient;

        public HttpBasedTransport(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            var requestWrapper = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
            };
            if (request.HttpMethod != null)
            {
                requestWrapper.Method = new HttpMethod(request.HttpMethod);
            }
            if (request.HttpVersion != null)
            {
                requestWrapper.Version = Version.Parse(request.HttpVersion);
            }
            if (connectionAllocationInfo?.RemoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint - hence no url authority specified");
            }
            var authority = (string)connectionAllocationInfo.RemoteEndpoint;
            string scheme = null;
            if (connectionAllocationInfo.ConnectivityParameters != null &&
                connectionAllocationInfo.ConnectivityParameters.ContainsKey("scheme"))
            {
                scheme = (string)connectionAllocationInfo.ConnectivityParameters["scheme"];
            }
            requestWrapper.RequestUri = new Uri($"{scheme ?? "http"}://{authority}{request.Path ?? ""}");
            if (request.Body != null)
            {
                requestWrapper.Content = new QuasiHttpBodyBackedHttpContent(request.Body);
                requestWrapper.Content.Headers.Add("Content-Type", request.Body.ContentType);
                if (request.Body.ContentLength >= 0)
                {
                    requestWrapper.Content.Headers.ContentLength = request.Body.ContentLength;
                }
                else
                {
                    requestWrapper.Headers.Add("Transfer-Encoding", "chunked");
                }
            }
            AddRequestHeaders(requestWrapper, request.Headers);

            var responseWrapper = await _httpClient.SendAsync(requestWrapper);

            var response = new DefaultQuasiHttpResponse
            {
                HttpVersion = responseWrapper.Version?.ToString(),
                HttpStatusCode = (int)responseWrapper.StatusCode,
                StatusMessage = responseWrapper.ReasonPhrase,
                StatusIndicatesSuccess = responseWrapper.IsSuccessStatusCode
            };
            if (response.HttpStatusCode >= 400 && response.HttpStatusCode < 500)
            {
                response.StatusIndicatesClientError = true;
            }
            if (responseWrapper.Content != null)
            {
                var responseStream = await responseWrapper.Content.ReadAsStreamAsync();
                var contentType = responseWrapper.Content.Headers.ContentType?.ToString();
                response.Body = new StreamBackedBody(responseStream, contentType);
            }
            response.Headers = new Dictionary<string, List<string>>();
            AdddResponseHeaders(response.Headers, responseWrapper);
            return response;
        }

        private void AddRequestHeaders(HttpRequestMessage request, IDictionary<string, List<string>> src)
        {
            if (src != null)
            {
                foreach (var entry in src)
                {
                    try
                    {
                        request.Headers.Add(entry.Key, entry.Value);
                    }
                    catch (InvalidOperationException)
                    {
                        // probably a content header.
                        if (request.Content != null)
                        {
                            try
                            {
                                request.Content.Headers.Add(entry.Key, entry.Value);
                            }
                            catch (InvalidOperationException)
                            {
                                // ignore.
                            }
                        }
                    }
                }
            }
        }

        private static void AdddResponseHeaders(IDictionary<string, List<string>> dest, HttpResponseMessage src)
        {
            foreach (var entry in src.Headers)
            {
                dest.Add(entry.Key, entry.Value.ToList());
            }
            if (src.Content != null)
            {
                foreach (var entry in src.Content.Headers)
                {
                    dest.Add(entry.Key, entry.Value.ToList());
                }
            }
        }
    }
}
