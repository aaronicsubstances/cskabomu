using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Kabomu.Abstractions;
using Kabomu.Impl;
using System.IO;
using System.Threading;

namespace Kabomu.Examples.Shared
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IQuasiHttpConnection"/> interface
    /// based on .NET sockets.
    /// </summary>
    public class SocketConnection : IQuasiHttpConnection
    {
        private static readonly IQuasiHttpProcessingOptions DefaultProcessingOptions =
            new DefaultQuasiHttpProcessingOptions();
        private readonly Socket _socket;
        private readonly Stream _reader;

        public SocketConnection(Socket socket, bool isClient,
            IQuasiHttpProcessingOptions processingOptions,
            IQuasiHttpProcessingOptions fallbackProcessingOptions = null)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            ProcessingOptions = MiscUtils.MergeProcessingOptions(processingOptions,
                fallbackProcessingOptions) ?? DefaultProcessingOptions;
            _reader = new SocketBackedStream(socket);
            TimeoutId = TransportImplHelpers.CreateCancellableTimeoutTask(
                ProcessingOptions.TimeoutMillis,
                isClient ? "send timeout" : "receive timeout");
            CancellationToken = TimeoutId?.CancellationTokenSource.Token ?? default;
        }

        public CancellablePromise TimeoutId { get; }
        public IQuasiHttpProcessingOptions ProcessingOptions { get; }
        public IDictionary<string, object> Environment { get; set; }
        public CancellationToken CancellationToken { get; }

        private async Task WriteSocketBytes(byte[] data, int offset, int length)
        {
            int totalBytesSent = 0;
            while (totalBytesSent < length)
            {
                int bytesSent = await _socket.SendAsync(
                    new ReadOnlyMemory<byte>(data,
                        offset + totalBytesSent,
                        length - totalBytesSent), SocketFlags.None);
                totalBytesSent += bytesSent;
            }
        }

        public Task Release(bool responseStreamingEnabled)
        {
            TimeoutId?.Cancel();
            if (responseStreamingEnabled)
            {
                return Task.CompletedTask;
            }
            _socket.Dispose();
            return Task.CompletedTask;
        }

        public async Task Write(bool isResponse, byte[] encodedHeaders,
            Stream body)
        {
            await WriteSocketBytes(encodedHeaders, 0, encodedHeaders.Length);
            if (body != null)
            {
                await MiscUtils.CopyBytesToSink(body, WriteSocketBytes, -1,
                    CancellationToken);
            }
        }

        public async Task<Stream> Read(bool isResponse,
            List<byte[]> encodedHeadersReceiver)
        {
            await QuasiHttpCodec.ReadEncodedHeaders(_reader,
                encodedHeadersReceiver, ProcessingOptions.MaxHeadersSize,
                CancellationToken);
            return _reader;
        }
    }
}
