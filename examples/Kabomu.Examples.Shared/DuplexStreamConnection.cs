using Kabomu.Abstractions;
using Kabomu.Exceptions;
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
        private readonly CancellationTokenSource _timeoutId;
        private readonly Task<IEncodedQuasiHttpEntity> _timeoutTask;

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

        public Task Release()
        {
            _timeoutId?.Cancel();
            _stream.Dispose();
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
            await MiscUtils.ReadBytesFully(_stream, encodedHeadersLength, 0,
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
            await MiscUtils.ReadBytesFully(_stream, headers, 0,
                headersLength);
            var body = _stream;
            if (isResponse && ProcessingOptions.ResponseBufferingEnabled != false)
            {
                body = new MemoryStream(await MiscUtils.ReadAllBytes(_stream,
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
