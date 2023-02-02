using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    internal class AltSendProtocolInternal : ISendProtocolInternal
    {
        private object _sendCancellationHandle;

        public AltSendProtocolInternal()
        {
        }

        public IQuasiHttpAltTransport TransportBypass { get; set; }
        public IConnectivityParams ConnectivityParams { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }
        public bool RequestWrappingEnabled { get; set; }
        public bool ResponseWrappingEnabled { get; set; }

        public void Cancel()
        {
            // reading these variables is thread safe if caller calls current method within same mutex as
            // Send().
            if (_sendCancellationHandle != null)
            {
                TransportBypass.CancelSendRequest(_sendCancellationHandle);
            }
            _sendCancellationHandle = null;
            TransportBypass = null;
            ConnectivityParams = null;
        }

        public async Task<ProtocolSendResult> Send(IQuasiHttpRequest request)
        {
            // assume properties are set correctly aside the transport.
            if (TransportBypass == null)
            {
                throw new MissingDependencyException("transport bypass");
            }

            // apply request wrapping if needed.
            if (RequestWrappingEnabled)
            {
                var requestBody = request.Body;
                if (requestBody != null)
                {
                    requestBody = new ProxyBody(requestBody);
                }
                request = new DefaultQuasiHttpRequest
                {
                    HttpVersion = request.HttpVersion,
                    Method = request.Method,
                    Target = request.Target,
                    Headers = request.Headers,
                    Body = requestBody
                };
            }

            var cancellableResTask = TransportBypass.ProcessSendRequest(request, ConnectivityParams);
            // writing this variable is thread safe if caller calls current method within same mutex as
            // Cancel().
            _sendCancellationHandle = cancellableResTask.Item2;

            // it is not a problem if this call exceeds timeout before returning, since
            // cancellation handle has already been saved within same mutex as Cancel(),
            // and so Cancel() will definitely see the cancellation handle and make use of it.
            IQuasiHttpResponse response = await cancellableResTask.Item1;
            IDirectSendResult sendResult = new DefaultDirectSendResult
            {
                Response = response
            };

            if (sendResult?.Response == null)
            {
                throw new ExpectationViolationException("no response");
            }

            response = sendResult.Response;

            var responseBody = response.Body;
            bool responseBufferingApplied = false;
            try
            {
                if (responseBody != null && ResponseBufferingEnabled && sendResult.ResponseBufferingApplied != true)
                {
                    // mark as applied here, so that if an error occurs,
                    // closing will still be done.
                    responseBufferingApplied = true;

                    // read response body into memory and create equivalent response for 
                    // which Close() operation is redundant.
                    responseBody = await ProtocolUtilsInternal.CreateEquivalentInMemoryBody(responseBody,
                        MaxChunkSize, ResponseBodyBufferingSizeLimit);
                    response = new DefaultQuasiHttpResponse
                    {
                        StatusCode = response.StatusCode,
                        Headers = response.Headers,
                        HttpVersion = response.HttpVersion,
                        HttpStatusMessage = response.HttpStatusMessage,
                        Body = responseBody
                    };
                }

                // apply response wrapping if needed.
                // NB: leverage wrapping done as a byproduct of applying response buffering.
                if (ResponseWrappingEnabled && !responseBufferingApplied)
                {
                    response = new ProxyQuasiHttpResponse(response);
                }

                return new ProtocolSendResult
                {
                    Response = response,
                    ResponseBufferingApplied = sendResult.ResponseBufferingApplied == true ||
                        responseBufferingApplied
                };
            }
            finally
            {
                if (responseBody == null || sendResult.ResponseBufferingApplied == true ||
                        responseBufferingApplied)
                {
                    // close original response.
                    await sendResult.Response.Close();
                }
            }
        }
    }
}
