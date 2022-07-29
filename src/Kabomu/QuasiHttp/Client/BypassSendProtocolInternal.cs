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

            // it is not a problem if this call exceeds timeout before returning, since
            // cancellation handle has already been saved within same mutex as Cancel(),
            // and so Cancel() will definitely see the cancellation handle and make use of it.
            IQuasiHttpResponse response = await cancellableResTask.Item1;

            if (response == null)
            {
                throw new Exception("no response");
            }

            if (!ResponseStreamingEnabled)
            {
                // if there is a response body, read it into memmory and create equivalent response for 
                // which Close() operation is redundant.
                // in any case make sure original response is closed.
                IQuasiHttpBody eqResponseBody = null;
                try
                {
                    if (response.Body != null)
                    {
                        eqResponseBody = await ProtocolUtilsInternal.CreateEquivalentInMemoryResponseBody(response.Body,
                            MaxChunkSize, ResponseBodyBufferingSizeLimit);
                    }
                }
                finally
                {
                    await response.Close();
                }
                if (response.Body != null)
                {
                    response = new DefaultQuasiHttpResponse
                    {
                        StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                        StatusIndicatesClientError = response.StatusIndicatesClientError,
                        StatusMessage = response.StatusMessage,
                        Headers = response.Headers,
                        HttpStatusCode = response.HttpStatusCode,
                        HttpVersion = response.HttpVersion,
                        Body = eqResponseBody
                    };
                }
            }

            await AbortCallback.Invoke(Parent, null, response);

            return response;
        }
    }
}
