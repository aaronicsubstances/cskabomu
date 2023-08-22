using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
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

        public QuasiHttpSendResponse ProcessSendRequest(
            object remoteEndpoint,
            IQuasiHttpRequest request,
            IQuasiHttpSendOptions sendOptions)
        {
            var resTask = ProcessSendRequestInternal(remoteEndpoint,
                request, sendOptions);
            return new QuasiHttpSendResponse
            {
                ResponseTask = resTask,
            };
        }

        public QuasiHttpSendResponse ProcessSendRequest(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions sendOptions)
        {
            var cts = new CancellationTokenSource();
            var resTask = ProcessSendRequestInternal(remoteEndpoint,
                requestFunc, sendOptions, cts);
            object sendCancellationHandle = cts;
            return new QuasiHttpSendResponse
            {
                ResponseTask = resTask,
                CancellationHandle = sendCancellationHandle
            };
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {
            if (sendCancellationHandle is CancellationTokenSource cts)
            {
                cts.Cancel();
            }
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestInternal(
            object remoteEndpoint,
            object requestOrRequestFunc,
            IQuasiHttpSendOptions sendOptions,
            CancellationTokenSource cancellationTokenSource = null)
        {
            IQuasiHttpRequest request;
            if (requestOrRequestFunc is IQuasiHttpRequest r)
            {
                request = r;
            }
            else {
                var requestFunc =
                    (Func<IDictionary<string, object>, Task<IQuasiHttpRequest>>)requestOrRequestFunc;
                request = await requestFunc.Invoke(null);
            }
            if (request == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }
            // todo: ensure disposal of request if it was retrieved
            // from externally supplied request func.
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
            requestWrapper.RequestUri = (Uri)remoteEndpoint;
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
                requestWrapper.Content.Headers.ContentLength = null;
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
                HttpStatusMessage = responseWrapper.ReasonPhrase
            };
            if (responseWrapper.Content != null)
            {
                var responseStream = await responseWrapper.Content.ReadAsStreamAsync();
                var contentLength = responseWrapper.Content.Headers.ContentLength ?? -1;
                response.Body = new LambdaBasedQuasiHttpBody
                {
                    ReaderFunc = () => responseStream,
                    ContentLength = contentLength
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
