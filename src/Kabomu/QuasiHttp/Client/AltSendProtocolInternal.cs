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

        public IQuasiHttpRequest Request { get; set; }
        public IQuasiHttpAltTransport TransportBypass { get; set; }
        public IConnectivityParams ConnectivityParams { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }

        public Task Cancel()
        {
            // reading these variables is thread safe if caller calls current method within same mutex as
            // Send().
            if (_sendCancellationHandle != null && TransportBypass != null)
            {
                // check for case in which TransportBypass was incorrectly set to null.
                TransportBypass.CancelSendRequest(_sendCancellationHandle);
            }
            return Task.CompletedTask;
        }

        public async Task<ProtocolSendResult> Send()
        {
            // assume properties are set correctly aside the transport.
            if (TransportBypass == null)
            {
                throw new MissingDependencyException("transport bypass");
            }
            if (Request == null)
            {
                throw new ExpectationViolationException("request");
            }

            IQuasiHttpRequest request = Request;

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
                throw new QuasiHttpRequestProcessingException(
                    QuasiHttpRequestProcessingException.ReasonCodeNoResponse,
                    "null response");
            }
            
            // save for closing later if needed.
            var originalResponse = response;
            try
            {
                var originalResponseBufferingApplied = ProtocolUtilsInternal.GetEnvVarAsBoolean(
                    response.Environment, TransportUtils.ResEnvKeyResponseBufferingApplied);

                var responseBody = response.Body;
                bool responseBufferingApplied = false;
                if (responseBody != null && ResponseBufferingEnabled && originalResponseBufferingApplied != true)
                {
                    // mark as applied here, so that if an error occurs,
                    // closing will still be done.
                    responseBufferingApplied = true;

                    // read response body into memory and create equivalent response for 
                    // which CustomDispose() operation is redundant.
                    responseBody = await ProtocolUtilsInternal.CreateEquivalentOfUnknownBodyInMemory(responseBody,
                        ResponseBodyBufferingSizeLimit);
                    response = new DefaultQuasiHttpResponse
                    {
                        StatusCode = response.StatusCode,
                        Headers = response.Headers,
                        HttpVersion = response.HttpVersion,
                        HttpStatusMessage = response.HttpStatusMessage,
                        Body = responseBody,
                        Environment = response.Environment
                    };
                }

                if (responseBody == null || originalResponseBufferingApplied == true ||
                        responseBufferingApplied)
                {
                    // close original response.
                    await originalResponse.CustomDispose();
                }

                return new ProtocolSendResult
                {
                    Response = response,
                    ResponseBufferingApplied = originalResponseBufferingApplied == true ||
                        responseBufferingApplied
                };
            }
            catch
            {
                try
                {
                    // don't wait.
                    _ = originalResponse.CustomDispose();
                }
                catch (Exception) { } // ignore
                throw;
            }
        }
    }
}
