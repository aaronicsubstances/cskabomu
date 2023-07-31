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
        public AltSendProtocolInternal()
        {
        }

        public object SendCancellationHandle { get; set; }
        public Task<IQuasiHttpResponse> ResponseTask { get; set; }
        public IQuasiHttpAltTransport TransportBypass { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }

        public Task Cancel()
        {
            if (SendCancellationHandle != null)
            {
                // check for case in which TransportBypass was incorrectly set to null.
                TransportBypass?.CancelSendRequest(SendCancellationHandle);
            }
            return Task.CompletedTask;
        }

        public async Task<ProtocolSendResultInternal> Send()
        {
            if (TransportBypass == null)
            {
                throw new MissingDependencyException("transport bypass");
            }
            if (ResponseTask == null)
            {
                throw new ExpectationViolationException("ResponseTask");
            }

            var response = await ResponseTask;

            if (response == null)
            {
                return null;
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

                return new ProtocolSendResultInternal
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
                    await originalResponse.CustomDispose();
                }
                catch (Exception) { } // ignore
                throw;
            }
        }
    }
}
