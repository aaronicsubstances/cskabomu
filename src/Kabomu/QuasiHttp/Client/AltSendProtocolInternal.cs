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
                request = ProtocolUtilsInternal.CloneQuasiHttpRequest(request, c => c.Body = requestBody);
            }

            var cancellableResTask = TransportBypass.ProcessSendRequest(request, ConnectivityParams);
            // writing this variable is thread safe if caller calls current method within same mutex as
            // Cancel().
            _sendCancellationHandle = cancellableResTask.Item2;

            // it is not a problem if this call exceeds timeout before returning, since
            // cancellation handle has already been saved within same mutex as Cancel(),
            // and so Cancel() will definitely see the cancellation handle and make use of it.
            var response = await cancellableResTask.Item1;

            if (response == null)
            {
                throw new ExpectationViolationException("no response");
            }
            
            // save for closing later if needed.
            var originalResponse = response;
            var originalResponseBufferingApplied = ProtocolUtilsInternal.GetEnvVarAsBoolean(
                response.Environment, TransportUtils.ResEnvKeyResponseBufferingApplied);

            var responseBody = response.Body;
            bool responseBufferingApplied = false;
            try
            {
                if (responseBody != null && ResponseBufferingEnabled && originalResponseBufferingApplied != true)
                {
                    // mark as applied here, so that if an error occurs,
                    // closing will still be done.
                    responseBufferingApplied = true;

                    // read response body into memory and create equivalent response for 
                    // which Close() operation is redundant.
                    responseBody = await ProtocolUtilsInternal.CreateEquivalentInMemoryBody(responseBody,
                        MaxChunkSize, ResponseBodyBufferingSizeLimit);
                    response = ProtocolUtilsInternal.CloneQuasiHttpResponse(response, c => c.Body = responseBody);
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
                    ResponseBufferingApplied = originalResponseBufferingApplied == true ||
                        responseBufferingApplied
                };
            }
            finally
            {
                if (responseBody == null || originalResponseBufferingApplied == true ||
                        responseBufferingApplied)
                {
                    // close original response.
                    await originalResponse.Close();
                }
            }
        }
    }
}
