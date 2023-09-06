using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.Impl;

namespace Kabomu
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
        private readonly CancellationTokenSource _timeoutId;
        private readonly Task<IEncodedReadRequest> _timeoutTask;

        public SocketConnection(Socket socket, bool isClient,
            IQuasiHttpProcessingOptions processingOptions,
            IQuasiHttpProcessingOptions fallbackProcessingOptions = null)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            if (processingOptions != null && fallbackProcessingOptions != null)
            {
                processingOptions = QuasiHttpUtils.MergeProcessingOptions(processingOptions,
                    fallbackProcessingOptions);
            }
            ProcessingOptions = (processingOptions ?? fallbackProcessingOptions)
                ?? DefaultProcessingOptions;
            Reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = async (data, offset, length) =>
                {
                    return await socket.ReceiveAsync(new Memory<byte>(data, offset, length),
                        SocketFlags.None);
                }
            };
            Writer = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = async (data, offset, length) =>
                {
                    int totalBytesSent = 0;
                    while (totalBytesSent < length)
                    {
                        int bytesSent = await socket.SendAsync(
                            new ReadOnlyMemory<byte>(data, offset + totalBytesSent, length - totalBytesSent), SocketFlags.None);
                        totalBytesSent += bytesSent;
                    }
                }
            };
            if (ProcessingOptions.TimeoutMillis > 0)
            {
                _timeoutId = new CancellationTokenSource();
                _timeoutTask = Task.Delay(ProcessingOptions.TimeoutMillis, _timeoutId.Token)
                    .ContinueWith<IEncodedReadRequest>(t =>
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

        public ICustomReader Reader { get; }
        public ICustomWriter Writer { get; }

        public IQuasiHttpProcessingOptions ProcessingOptions { get; }

        public IDictionary<string, object> Environment { get; set; }

        public Task Release()
        {
            _timeoutId?.Cancel();
            _socket.Dispose();
            return Task.CompletedTask;
        }

        public async Task Write(bool isResponse,
            byte[] encodedHeaders, object bodyReader)
        {
            var mainTask = WriteInternal(isResponse, encodedHeaders, bodyReader);
            if (_timeoutTask != null)
            {
                await await Task.WhenAny(mainTask, _timeoutTask);
            }
            await mainTask;
        }

        private async Task WriteInternal(bool isResponse,
            byte[] encodedHeaders, object bodyReader)
        {
            await Writer.WriteBytes(encodedHeaders, 0, encodedHeaders.Length);
            if (bodyReader != null)
            {
                await QuasiHttpUtils.CopyBytes(bodyReader, Writer);
            }
            if (isResponse)
            {
                await Release();
            }
        }

        public async Task<IEncodedReadRequest> Read(bool isResponse)
        {
            var mainTask = ReadInternal(isResponse);
            if (_timeoutTask != null)
            {
                await await Task.WhenAny(mainTask, _timeoutTask);
            }
            return await mainTask;
        }

        private async Task<IEncodedReadRequest> ReadInternal(bool isResponse)
        {
            var encodedHeadersLength = new byte[
                QuasiHttpCodec.LengthOfEncodedHeadersLength];
            await QuasiHttpUtils.ReadBytesFully(Reader, encodedHeadersLength, 0,
                encodedHeadersLength.Length);
            int headersLength = int.Parse(Encoding.ASCII.GetString(
                encodedHeadersLength));
            if (headersLength < 0)
            {
                throw new ChunkDecodingException(
                    "invalid length encountered for quasi http headers: " +
                    $"{headersLength}");
            }
            int maxHeadersSize = ProcessingOptions.MaxHeadersSize;
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = QuasiHttpCodec.DefaultMaxHeadersSize;
            }
            if (headersLength > maxHeadersSize)
            {
                throw new ChunkDecodingException("quasi http headers exceed max " +
                    $"({headersLength} > {ProcessingOptions.MaxHeadersSize})");
            }
            var headers = new byte[headersLength];
            await QuasiHttpUtils.ReadBytesFully(Reader, headers, 0,
                headersLength);
            object body = Reader;
            if (isResponse && ProcessingOptions.ResponseBufferingEnabled != false)
            {
                body = await QuasiHttpUtils.ReadAllBytes(Reader,
                    ProcessingOptions.ResponseBodyBufferingSizeLimit);
                await Release();
            }
            return new DefaultEncodedReadRequest
            {
                Headers = headers,
                Body = body
            };
        }
    }
}
