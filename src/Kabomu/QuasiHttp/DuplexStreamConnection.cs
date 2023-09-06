﻿using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public class DuplexStreamConnection : IQuasiTcpConnection
    {
        private static readonly IQuasiHttpProcessingOptions DefaultSendOptions =
            new DefaultQuasiHttpProcessingOptions();
        private readonly Stream _stream;
        private readonly CancellationTokenSource _timeoutId;
        private readonly Task<IEncodedReadRequest> _timeoutTask;

        public DuplexStreamConnection(Stream stream, bool isClient,
            IQuasiHttpProcessingOptions processingOptions,
            IQuasiHttpProcessingOptions fallbackProcessingOptions = null)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            if (processingOptions != null && fallbackProcessingOptions != null)
            {
                processingOptions = IOUtils.MergeProcessingOptions(processingOptions,
                    fallbackProcessingOptions);
            }
            ProcessingOptions = (processingOptions ?? fallbackProcessingOptions)
                ?? DefaultSendOptions;
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

        public Stream Reader => _stream;
        public Stream Writer => _stream;

        public IQuasiHttpProcessingOptions ProcessingOptions { get; }

        public IDictionary<string, object> Environment { get; set; }

        public Task Release()
        {
            _timeoutId?.Cancel();
            _stream.Dispose();
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
            await Writer.WriteAsync(encodedHeaders, 0, encodedHeaders.Length);
            if (bodyReader != null)
            {
                await IOUtils.CopyBytes(bodyReader, Writer);
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
                QuasiHttpHeadersCodec.LengthOfEncodedHeadersLength];
            await IOUtils.ReadBytesFully(Reader, encodedHeadersLength, 0,
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
                maxHeadersSize = QuasiHttpHeadersCodec.DefaultMaxHeadersSize;
            }
            if (headersLength > maxHeadersSize)
            {
                throw new ChunkDecodingException("quasi http headers exceed max " +
                    $"({headersLength} > {ProcessingOptions.MaxHeadersSize})");
            }
            var headers = new byte[headersLength];
            await IOUtils.ReadBytesFully(Reader, headers, 0,
                headersLength);
            object body = Reader;
            if (isResponse && ProcessingOptions.ResponseBufferingEnabled != false)
            {
                body = await IOUtils.ReadAllBytes(Reader,
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
