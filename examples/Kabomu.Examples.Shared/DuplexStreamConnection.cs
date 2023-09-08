﻿using Kabomu.Abstractions;
using Kabomu.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IQuasiHttpConnection"/> interface
    /// based on .NET Streams.
    /// </summary>
    public class DuplexStreamConnection : IQuasiHttpConnection
    {
        private static readonly IQuasiHttpProcessingOptions DefaultProcessingOptions =
            new DefaultQuasiHttpProcessingOptions();
        private readonly Stream _stream;

        public DuplexStreamConnection(Stream stream, bool isClient,
            IQuasiHttpProcessingOptions processingOptions,
            IQuasiHttpProcessingOptions fallbackProcessingOptions = null)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            if (processingOptions != null && fallbackProcessingOptions != null)
            {
                processingOptions = QuasiHttpProtocolUtils.MergeProcessingOptions(processingOptions,
                    fallbackProcessingOptions);
            }
            ProcessingOptions = (processingOptions ?? fallbackProcessingOptions)
                ?? DefaultProcessingOptions;
            TimeoutId = TransportImplHelpers.CreateCancellableTimeoutTask(
                ProcessingOptions.TimeoutMillis,
                isClient ? "send timeout" : "receive timeout");
        }

        public CancellablePromise TimeoutId { get; }

        public IQuasiHttpProcessingOptions ProcessingOptions { get; }

        public IDictionary<string, object> Environment { get; set; }

        public async Task Release()
        {
            TimeoutId?.Cancel();
            await _stream.DisposeAsync();
        }

        public async Task Write(bool isResponse, IEncodedQuasiHttpEntity entity)
        {
            var mainTask = WriteInternal(isResponse, entity);
            await MiscUtils.CompleteMainTask(mainTask, TimeoutId?.Task);
        }

        private async Task WriteInternal(bool isResponse, IEncodedQuasiHttpEntity entity)
        {
            var encodedHeaders = entity.Headers;
            await _stream.WriteAsync(encodedHeaders, 0, encodedHeaders.Length);
            if (entity.Body != null)
            {
                await MiscUtils.CopyBytesToStream(entity.Body, _stream);
            }
            if (isResponse)
            {
                await Release();
            }
        }

        public async Task<IEncodedQuasiHttpEntity> Read(bool isResponse)
        {
            var mainTask = ReadInternal(isResponse);
            return await MiscUtils.CompleteMainTask(mainTask, TimeoutId?.Task);
        }

        private async Task<IEncodedQuasiHttpEntity> ReadInternal(bool isResponse)
        {
            var headers = await TransportImplHelpers.ReadHeaders(_stream,
                ProcessingOptions);
            var body = _stream;
            if (isResponse)
            {
                if (ProcessingOptions.ResponseBufferingEnabled != false)
                {
                    body = new MemoryStream(await MiscUtils.ReadAllBytes(_stream,
                        ProcessingOptions.ResponseBodyBufferingSizeLimit));
                    await Release();
                }
                else
                {
                    // partially release resources.
                    TimeoutId?.Cancel();
                }
            }
            return new DefaultEncodedQuasiHttpEntity
            {
                Headers = headers,
                Body = body
            };
        }
    }
}
