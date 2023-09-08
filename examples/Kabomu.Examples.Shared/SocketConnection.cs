using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Kabomu.Abstractions;
using Kabomu.Impl;
using System.IO;

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

            if (processingOptions != null && fallbackProcessingOptions != null)
            {
                processingOptions = QuasiHttpProtocolUtils.MergeProcessingOptions(processingOptions,
                    fallbackProcessingOptions);
            }
            ProcessingOptions = (processingOptions ?? fallbackProcessingOptions)
                ?? DefaultProcessingOptions;
            var chunkGenerator = MiscUtils.CreateGeneratorFromSource(
                async (data, offset, length) =>
                {
                    return await socket.ReceiveAsync(new Memory<byte>(data, offset, length),
                        SocketFlags.None);
                }
            );
            _reader = MiscUtils.CreateInputStreamFromGenerator(chunkGenerator);
            TimeoutId = TransportImplHelpers.CreateCancellableTimeoutTask(
                ProcessingOptions.TimeoutMillis,
                isClient ? "send timeout" : "receive timeout");
        }

        public CancellablePromise TimeoutId { get; }

        public IQuasiHttpProcessingOptions ProcessingOptions { get; }

        public IDictionary<string, object> Environment { get; set; }

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

        public async Task Write(bool isResponse, IEncodedQuasiHttpEntity entity)
        {
            var mainTask = WriteInternal(entity);
            await MiscUtils.CompleteMainTask(mainTask, TimeoutId?.Task);
        }

        private async Task WriteInternal(IEncodedQuasiHttpEntity entity)
        {
            var encodedHeaders = entity.Headers;
            await WriteSocketBytes(encodedHeaders, 0, encodedHeaders.Length);
            if (entity.Body != null)
            {
                await MiscUtils.CopyBytesToSink(entity.Body, WriteSocketBytes);
            }
        }

        public async Task<IEncodedQuasiHttpEntity> Read(bool isResponse)
        {
            var mainTask = ReadInternal();
            return await MiscUtils.CompleteMainTask(mainTask, TimeoutId?.Task);
        }

        private async Task<IEncodedQuasiHttpEntity> ReadInternal()
        {
            var headers = await TransportImplHelpers.ReadHeaders(_reader,
                ProcessingOptions);
            var body = _reader;
            return new DefaultEncodedQuasiHttpEntity
            {
                Headers = headers,
                Body = body
            };
        }

        public async Task<Stream> ApplyResponseBuffering(Stream body)
        {
            return new MemoryStream(await MiscUtils.ReadAllBytes(body,
                ProcessingOptions.ResponseBodyBufferingSizeLimit));
        }
    }
}
