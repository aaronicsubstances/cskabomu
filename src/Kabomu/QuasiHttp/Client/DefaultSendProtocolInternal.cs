﻿using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    internal class DefaultSendProtocolInternal : ISendProtocolInternal
    {
        public DefaultSendProtocolInternal()
        {
        }

        public IQuasiHttpRequest Request { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }
        public bool EnsureNonNullResponse { get; set; }

        public async Task Cancel()
        {
            // just in case Transport was incorrectly set to null.
            if (Transport != null)
            {
                await Transport.ReleaseConnection(Connection);
            }
        }

        public async Task<ProtocolSendResultInternal> Send()
        {
            if (Transport == null)
            {
                throw new MissingDependencyException("client transport");
            }
            if (Request == null)
            {
                throw new ExpectationViolationException("request");
            }

            var writer = Transport.GetWriter(Connection);

            // send lead chunk first, before racing sending of request body
            // and receiving of response.
            await SendRequestLeadChunk(Request, writer);
            var reqTransferTask = ProtocolUtilsInternal.TransferBodyToTransport(
                writer, MaxChunkSize, Request.Body);
            var resFetchTask = StartFetchingResponse();
            if (await Task.WhenAny(reqTransferTask, resFetchTask) == reqTransferTask)
            {
                // let any request transfer exceptions terminate entire processing.
                await reqTransferTask;
            }
            return await resFetchTask;
        }

        private async Task SendRequestLeadChunk(IQuasiHttpRequest request,
            object writer)
        {
            var chunk = LeadChunk.CreateFromRequest(request);
            await ChunkedTransferUtils.WriteLeadChunk(writer, chunk, MaxChunkSize);
        }

        private async Task<ProtocolSendResultInternal> StartFetchingResponse()
        {
            var reader = Transport.GetReader(Connection);
            var chunk = await ChunkedTransferUtils.ReadLeadChunk(reader,
                MaxChunkSize);
            if (chunk == null)
            {
                if (EnsureNonNullResponse)
                {
                    throw new QuasiHttpRequestProcessingException("no response");
                }
                return null;
            }
            var response = new DefaultQuasiHttpResponse();
            chunk.UpdateResponse(response);
            Func<Task> releaseFunc = () => Transport.ReleaseConnection(Connection);
            response.Body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                reader, chunk.ContentLength, releaseFunc,
                MaxChunkSize, ResponseBufferingEnabled,
                ResponseBodyBufferingSizeLimit);

            return new ProtocolSendResultInternal
            {
                Response = response,
                ResponseBufferingApplied = ResponseBufferingEnabled
            };
        }
    }
}
