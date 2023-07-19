using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class HttpBasedTransport : IQuasiHttpAltTransport
    {
        private readonly HttpClient _httpClient;

        public HttpBasedTransport(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(IQuasiHttpRequest request,
            IConnectivityParams connectivityParams)
        {
            return ProcessSendRequest(_ => Task.FromResult(request), connectivityParams);
        }

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IConnectivityParams connectivityParams)
        {
            var cts = new CancellationTokenSource();
            var resTask = ProcessSendRequestInternal(requestFunc, connectivityParams, cts);
            object sendCancellationHandle = cts;
            return (resTask, sendCancellationHandle);
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {
            if (sendCancellationHandle is CancellationTokenSource cts)
            {
                cts.Cancel();
            }
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestInternal(
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IConnectivityParams connectivityParams, CancellationTokenSource cancellationTokenSource)
        {
            var request = await requestFunc.Invoke(null);
            if (request == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }
            var requestWrapper = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
            };
            if (request.Method != null)
            {
                requestWrapper.Method = new HttpMethod(request.Method);
            }
            if (request.HttpVersion != null)
            {
                requestWrapper.Version = Version.Parse(request.HttpVersion);
            }
            requestWrapper.RequestUri = (Uri)connectivityParams.RemoteEndpoint;
            if (request.Target != null)
            {
                requestWrapper.RequestUri = new Uri(requestWrapper.RequestUri, request.Target);
            }
            if (request.Body != null)
            {
                requestWrapper.Content = new QuasiHttpBodyBackedHttpContent(request.Body);
            }
            // add request headers from caller before overriding key headers.
            AddRequestHeaders(requestWrapper, request.Headers);
            requestWrapper.Headers.TransferEncoding.Clear();
            if (requestWrapper.Content != null)
            {
                requestWrapper.Content.Headers.ContentType = null;
                requestWrapper.Content.Headers.ContentLength = null;
                if (request.Body.ContentType != null)
                {
                    requestWrapper.Content.Headers.Add("Content-Type", request.Body.ContentType);
                }
                if (request.Body.ContentLength >= 0)
                {
                    requestWrapper.Content.Headers.ContentLength = request.Body.ContentLength;
                }
                else
                {
                    requestWrapper.Headers.TransferEncodingChunked = true;
                }
            }

            var responseWrapper = await _httpClient.SendAsync(requestWrapper, cancellationTokenSource.Token);

            var response = new DefaultQuasiHttpResponse
            {
                HttpVersion = responseWrapper.Version?.ToString(),
                StatusCode = (int)responseWrapper.StatusCode,
                HttpStatusMessage = responseWrapper.ReasonPhrase,
                CancellationTokenSource = cancellationTokenSource
            };
            if (responseWrapper.Content != null)
            {
                var responseStream = await responseWrapper.Content.ReadAsStreamAsync();
                var contentLength = responseWrapper.Content.Headers.ContentLength ?? -1;
                var contentType = responseWrapper.Content.Headers.ContentType?.ToString();
                response.Body = new CustomReaderBackedBody(
                    new StreamCustomReaderWriter(responseStream))
                {
                    ContentLength = contentLength,
                    ContentType = contentType
                };
            }
            response.Headers = new Dictionary<string, IList<string>>();
            AdddResponseHeaders(response.Headers, responseWrapper);
            return response;
        }

        private void AddRequestHeaders(HttpRequestMessage request, IDictionary<string, IList<string>> src)
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

        private static void AdddResponseHeaders(IDictionary<string, IList<string>> dest, HttpResponseMessage src)
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
