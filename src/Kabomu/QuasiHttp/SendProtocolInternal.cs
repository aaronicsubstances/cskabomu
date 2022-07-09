using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class SendProtocolInternal
    {
        private bool _cancelled;
        private IQuasiHttpBody _requestBody, _responseBody;

        public SendProtocolInternal()
        {
        }

        public SendTransferInternal Parent { get; set; }
        public Func<SendTransferInternal, Exception, Task> AbortCallback { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public IMutexApi MutexApi { get; set; }

        public async Task Cancel()
        {
            Task reqBodyEndTask = null, resBodyEndTask = null, releaseTask = null;
            using (await MutexApi.Synchronize())
            {
                /* always cancel so that if no mutex api is being used,
                 * at least the transfer connection will be closed, in order
                 * to forcefully abort the transfer.
                 */
                //if (_cancelled)
                //{
                //    return;
                //}
                reqBodyEndTask = _requestBody?.EndRead();
                resBodyEndTask = _responseBody?.EndRead();
                if (Connection != null)
                {
                    releaseTask = Transport.ReleaseConnection(Connection);
                }

                _cancelled = true;
            }
            if (reqBodyEndTask != null)
            {
                await reqBodyEndTask;
            }
            if (resBodyEndTask != null)
            {
                await resBodyEndTask;
            }
            if (releaseTask != null)
            {
                await releaseTask;
            }
        }

        public Task<IQuasiHttpResponse> Send(IQuasiHttpRequest request)
        {
            // assume properties are set correctly aside the transport.
            if (Transport == null)
            {
                throw new MissingDependencyException("client transport");
            }
            return SendRequestLeadChunk(request);
        }

        private async Task<IQuasiHttpResponse> SendRequestLeadChunk(IQuasiHttpRequest request)
        {
            Task writeTask;
            using (await MutexApi.Synchronize())
            {
                var chunk = new LeadChunk
                {
                    Version = LeadChunk.Version01,
                    Path = request.Path,
                    Headers = request.Headers,
                    HttpVersion = request.HttpVersion,
                    HttpMethod = request.HttpMethod
                };
                _requestBody = request.Body;
                if (request.Body != null)
                {
                    chunk.ContentLength = request.Body.ContentLength;
                    chunk.ContentType = request.Body.ContentType;
                }
                writeTask = ChunkEncodingBody.WriteLeadChunk(Transport, Connection,
                    MaxChunkSize, chunk);
            }

            await writeTask;

            Task <IQuasiHttpResponse> responseFetchTask;
            using (await MutexApi.Synchronize())
            {
                if (_cancelled)
                {
                    return null;
                }

                if (_requestBody != null)
                {
                    if (_requestBody.ContentLength < 0)
                    {
                        _requestBody = new ChunkEncodingBody(_requestBody, MaxChunkSize);
                    }
                    // let any error be handled by abort callback, or
                    // TaskScheduler.UnobservedTaskException as a last resort.
                    _ = TransferRequestBodyToTransport();
                }

                responseFetchTask = StartFetchingResponse();
            }

            return await responseFetchTask;
        }

        private async Task TransferRequestBodyToTransport()
        {
            try
            {
                await TransportUtils.TransferBodyToTransport(Transport,
                    Connection, _requestBody, MaxChunkSize);
            }
            catch (Exception e)
            {
                await AbortCallback.Invoke(Parent, e);
            }
        }

        private async Task<IQuasiHttpResponse> StartFetchingResponse()
        {
            var chunk = await ChunkDecodingBody.ReadLeadChunk(Transport, Connection,
                MaxChunkSize);

            Task abortTask = null;
            DefaultQuasiHttpResponse response;
            using (await MutexApi.Synchronize())
            {
                if (_cancelled)
                {
                    return null;
                }

                response = new DefaultQuasiHttpResponse
                {
                    StatusIndicatesSuccess = chunk.StatusIndicatesSuccess,
                    StatusIndicatesClientError = chunk.StatusIndicatesClientError,
                    StatusMessage = chunk.StatusMessage,
                    Headers = chunk.Headers,
                    HttpVersion = chunk.HttpVersion,
                    HttpStatusCode = chunk.HttpStatusCode
                };

                if (chunk.ContentLength != 0)
                {
                    var transportBody = new TransportBackedBody(Transport, Connection,
                        CloseTransfer, chunk.ContentLength, chunk.ContentType);
                    if (chunk.ContentLength < 0)
                    {
                        response.Body = new ChunkDecodingBody(transportBody, MaxChunkSize);
                    }
                    else
                    {
                        response.Body = transportBody;
                    }
                    _responseBody = response.Body;

                    // close request body nonetheless
                    abortTask = _requestBody?.EndRead();
                }
                else
                {
                    abortTask = AbortCallback.Invoke(Parent, null);
                }
            }

            if (abortTask != null)
            {
                await abortTask;
            }

            return response;
        }

        private async Task CloseTransfer()
        {
            Task abortTask;
            using (await MutexApi.Synchronize())
            {
                if (_cancelled)
                {
                    return;
                }
                abortTask = AbortCallback.Invoke(Parent, null);
            }
            await abortTask;
        }
    }
}
