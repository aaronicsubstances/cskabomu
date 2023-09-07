using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Kabomu.Abstractions;
using Kabomu.Exceptions;
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
        private readonly CancellationTokenSource _timeoutId;
        private readonly Task<IEncodedQuasiHttpEntity> _timeoutTask;

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
                ;
            if (ProcessingOptions.TimeoutMillis > 0)
            {
                _timeoutId = new CancellationTokenSource();
                _timeoutTask = Task.Delay(ProcessingOptions.TimeoutMillis, _timeoutId.Token)
                    .ContinueWith<IEncodedQuasiHttpEntity>(t =>
                    {
                        if (!t.IsCanceled)
                        {
                            throw new QuasiHttpRequestProcessingException(
                                isClient ? "send timeout" : "receive timeout",
                                QuasiHttpRequestProcessingException.ReasonCodeTimeout);
                        }
                        return null;
                    });
            }
            else
            {
                _timeoutId = null;
                _timeoutTask = null;
            }
        }

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

        public Task Release()
        {
            _timeoutId?.Cancel();
            _socket.Dispose();
            return Task.CompletedTask;
        }

        public async Task Write(bool isResponse, IEncodedQuasiHttpEntity entity)
        {
            var mainTask = WriteInternal(isResponse, entity);
            if (_timeoutTask != null)
            {
                await await Task.WhenAny(mainTask, _timeoutTask);
            }
            await mainTask;
        }

        private async Task WriteInternal(bool isResponse, IEncodedQuasiHttpEntity entity)
        {
            var encodedHeaders = entity.Headers;
            await WriteSocketBytes(encodedHeaders, 0, encodedHeaders.Length);
            if (entity.Body != null)
            {
                await MiscUtils.CopyBytesToSink(entity.Body, WriteSocketBytes);
            }
            if (isResponse)
            {
                await Release();
            }
        }

        public async Task<IEncodedQuasiHttpEntity> Read(bool isResponse)
        {
            var mainTask = ReadInternal(isResponse);
            if (_timeoutTask != null)
            {
                await await Task.WhenAny(mainTask, _timeoutTask);
            }
            return await mainTask;
        }

        private async Task<IEncodedQuasiHttpEntity> ReadInternal(bool isResponse)
        {
            var encodedHeadersLength = new byte[
                QuasiHttpProtocolUtils.LengthOfEncodedHeadersLength];
            await MiscUtils.ReadBytesFully(_reader, encodedHeadersLength, 0,
                encodedHeadersLength.Length);
            int headersLength = MiscUtils.ParseInt32(
                MiscUtils.BytesToString(encodedHeadersLength));
            if (headersLength < 0)
            {
                throw new ChunkDecodingException(
                    "invalid length encountered for quasi http headers: " +
                    $"{headersLength}");
            }
            int maxHeadersSize = ProcessingOptions.MaxHeadersSize;
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = QuasiHttpProtocolUtils.DefaultMaxHeadersSize;
            }
            if (headersLength > maxHeadersSize)
            {
                throw new ChunkDecodingException("quasi http headers exceed max " +
                    $"({headersLength} > {ProcessingOptions.MaxHeadersSize})");
            }
            var headers = new byte[headersLength];
            await MiscUtils.ReadBytesFully(_reader, headers, 0,
                headersLength);
            var body = _reader;
            if (isResponse && ProcessingOptions.ResponseBufferingEnabled != false)
            {
                body = new MemoryStream(await MiscUtils.ReadAllBytes(_reader,
                    ProcessingOptions.ResponseBodyBufferingSizeLimit));
                await Release();
            }
            return new DefaultEncodedQuasiHttpEntity
            {
                Headers = headers,
                Body = body
            };
        }
    }
}
