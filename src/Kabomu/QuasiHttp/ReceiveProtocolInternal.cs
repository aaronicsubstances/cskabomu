﻿using Kabomu.Common;
using Kabomu.Common.Bodies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ReceiveProtocolInternal : ITransferProtocolInternal
    {
        private readonly object _lock;
        private IQuasiHttpBody _requestBody, _responseBody;

        public ReceiveProtocolInternal(object lockObj)
        {
            _lock = lockObj;
        }

        public IParentTransferProtocolInternal Parent { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }
        public bool IsAborted { get; set; }
        public CancellationTokenSource TimeoutCancellationHandle { get; set; }

        public async Task Cancel(Exception e)
        {
            Task reqBodyEndTask = null, resBodyEndTask = null;
            lock (_lock)
            {
                if (_requestBody != null)
                {
                    reqBodyEndTask = _requestBody.EndRead(e);
                }
                if (_responseBody != null)
                {
                    resBodyEndTask = _responseBody.EndRead(e);
                }
            }
            if (reqBodyEndTask != null)
            {
                await reqBodyEndTask;
            }
            if (resBodyEndTask != null)
            {
                await resBodyEndTask;
            }
        }

        public Task Receive()
        {
            return ReadRequestLeadChunk();
        }

        private async Task ReadRequestLeadChunk()
        {
            Task<LeadChunk> readTask;
            lock (_lock)
            {
                readTask = ChunkDecodingBody.ReadLeadChunk(Parent.Transport, Connection, MaxChunkSize);
            }

            var chunk = await readTask;

            Task<IQuasiHttpResponse> appTask;
            lock (_lock)
            {
                if (IsAborted)
                {
                    return;
                }

                var request = new DefaultQuasiHttpRequest
                {
                    Path = chunk.Path,
                    Headers = chunk.Headers,
                    HttpVersion = chunk.HttpVersion,
                    HttpMethod = chunk.HttpMethod
                };
                if (chunk.ContentLength != 0)
                {
                    var transportBody = new TransportBackedBody(Parent.Transport, Connection,
                        null, chunk.ContentLength, chunk.ContentType);
                    if (chunk.ContentLength < 0)
                    {
                        request.Body = new ChunkDecodingBody(transportBody, MaxChunkSize);
                    }
                    else
                    {
                        request.Body = transportBody;
                    }
                }
                _requestBody = request.Body;
                appTask = Parent.Application.ProcessRequest(request,
                    RequestEnvironment ?? new Dictionary<string, object>());
            }

            var response = await appTask;

            Task sendTask;
            lock (_lock)
            {
                if (IsAborted)
                {
                    return;
                }
                if (response == null)
                {
                    throw new Exception("no response");
                }
                sendTask = SendResponseLeadChunk(response);
            }

            await sendTask;
        }

        private async Task SendResponseLeadChunk(IQuasiHttpResponse response)
        {
            var chunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers,
                HttpVersion = response.HttpVersion,
                HttpStatusCode = response.HttpStatusCode
            };

            _responseBody = response.Body;
            if (response.Body != null)
            {
                chunk.ContentLength = response.Body.ContentLength;
                chunk.ContentType = response.Body.ContentType;
            }

            await ChunkEncodingBody.WriteLeadChunk(Parent.Transport, Connection,
                MaxChunkSize, chunk);

            Task bodyTransferTask = null, abortTask = null;
            lock (_lock)
            {
                if (IsAborted)
                {
                    return;
                }
                if (_responseBody != null)
                {
                    if (_responseBody.ContentLength < 0)
                    {
                        _responseBody = new ChunkEncodingBody(_responseBody, MaxChunkSize);
                    }
                    bodyTransferTask = TransportUtils.TransferBodyToTransport(Parent.Transport,
                        Connection, _responseBody, MaxChunkSize);
                }
                else
                {
                    abortTask = Parent.AbortTransfer(this, null);
                }
            }

            if (bodyTransferTask != null)
            {
                await bodyTransferTask;
            }

            if (abortTask != null)
            {
                await abortTask;
            }
        }
    }
}
