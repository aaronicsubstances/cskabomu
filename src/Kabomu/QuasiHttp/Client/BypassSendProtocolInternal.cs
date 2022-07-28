using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    internal class BypassSendProtocolInternal : ISendProtocolInternal
    {
        private object _sendCancellationHandle;

        public object Parent { get; set; }
        public Func<object, Exception, IQuasiHttpResponse, Task> AbortCallback { get; set; }
        public IQuasiHttpTransportBypass TransportBypass { get; set; }
        public IConnectivityParams ConnectivityParams { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseStreamingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }

        public BypassSendProtocolInternal()
        {
        }

        public Task Cancel()
        {
            // reading these variables is thread safe if caller calls current method within same mutex as
            // Send().
            if (_sendCancellationHandle != null)
            {
                TransportBypass.CancelSendRequest(_sendCancellationHandle);
            }
            return Task.CompletedTask;
        }

        public async Task<IQuasiHttpResponse> Send(IQuasiHttpRequest request)
        {
            // assume properties are set correctly aside the transport.
            if (TransportBypass == null)
            {
                throw new MissingDependencyException("transport bypass");
            }

            var cancellableResTask = TransportBypass.ProcessSendRequest(request, ConnectivityParams);
            // writing this variable is thread safe if caller calls current method within same mutex as
            // Cancel().
            _sendCancellationHandle = cancellableResTask.Item2;

            IQuasiHttpResponse response = await cancellableResTask.Item1;

            if (response == null)
            {
                throw new Exception("no response");
            }
            if (response.Body != null && !ResponseStreamingEnabled)
            {
                // read in entirety of response body into memory, and respect content length for
                // the sake of tests.
                if (response.Body.ContentLength > 0 && response.Body.ContentLength > ResponseBodyBufferingSizeLimit)
                {
                    throw new BodySizeLimitExceededException(ResponseBodyBufferingSizeLimit,
                        $"content length larger than buffering limit of " +
                        $"{ResponseBodyBufferingSizeLimit} bytes", null);
                }
                var inMemStream = await TransportUtils.ReadBodyToMemoryStream(response.Body, MaxChunkSize,
                    ResponseBodyBufferingSizeLimit);
                var newResponseBody = new StreamBackedBody(inMemStream, response.Body.ContentLength)
                {
                    ContentType = response.Body.ContentType
                };
                response = CreateEquivalentResponse(response, newResponseBody);
            }

            await AbortCallback.Invoke(Parent, null, response);

            return response;
        }

        private static IQuasiHttpResponse CreateEquivalentResponse(IQuasiHttpResponse res, IQuasiHttpBody newResponseBody)
        {
            var newResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = res.StatusIndicatesSuccess,
                StatusIndicatesClientError = res.StatusIndicatesClientError,
                StatusMessage = res.StatusMessage,
                Headers = res.Headers,
                HttpStatusCode = res.HttpStatusCode,
                HttpVersion = res.HttpVersion,
                Body = newResponseBody                
            };
            return newResponse;
        }
    }
}
