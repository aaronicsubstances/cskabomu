using Kabomu.Abstractions;
using Kabomu.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            ProcessingOptions = MiscUtils.MergeProcessingOptions(processingOptions,
                fallbackProcessingOptions) ?? DefaultProcessingOptions;
            TimeoutId = TransportImplHelpers.CreateCancellableTimeoutTask(
                ProcessingOptions.TimeoutMillis,
                isClient ? "send timeout" : "receive timeout");
            CancellationToken = TimeoutId?.CancellationTokenSource.Token ?? default;
        }

        public CancellablePromise TimeoutId { get; }
        public IQuasiHttpProcessingOptions ProcessingOptions { get; }
        public IDictionary<string, object> Environment { get; set; }
        public CancellationToken CancellationToken { get; }

        public async Task Release(bool responseStreamingEnabled)
        {
            TimeoutId?.Cancel();
            if (responseStreamingEnabled)
            {
                return;
            }
            await _stream.DisposeAsync();
        }

        public async Task Write(bool isResponse,
            byte[] encodedHeaders, Stream body)
        {
            await _stream.WriteAsync(encodedHeaders, 0, encodedHeaders.Length);
            if (body != null)
            {
                await MiscUtils.CopyBytesToStream(body, _stream,
                    CancellationToken);
            }
        }

        public async Task<Stream> Read(bool isResponse,
            List<byte[]> encodedHeadersReceiver)
        {
            await QuasiHttpCodec.ReadEncodedHeaders(_stream,
                encodedHeadersReceiver, ProcessingOptions.MaxHeadersSize,
                CancellationToken);
            return _stream;
        }
    }
}
