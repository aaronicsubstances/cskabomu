using Kabomu.Abstractions;
using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public static class TransportImplHelpers
    {
        public static CancellablePromise CreateCancellableTimeoutTask(
            int timeoutMillis, string timeoutMsg)
        {
            if (timeoutMillis <= 0)
            {
                return null;
            }
            var timeoutId = new CancellationTokenSource();
            var timeoutTask = Task.Delay(timeoutMillis, timeoutId.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        var timeoutError = new QuasiHttpRequestProcessingException(timeoutMsg,
                            QuasiHttpRequestProcessingException.ReasonCodeTimeout);
                        throw timeoutError;
                    }
                });
            return new CancellablePromise
            {
                Task = timeoutTask,
                CancellationTokenSource = timeoutId
            };
        }

        public static async Task<byte[]> ReadHeaders(Stream stream,
            IQuasiHttpProcessingOptions options)
        {
            var encodedHeadersLength = new byte[
                QuasiHttpProtocolUtils.LengthOfEncodedHeadersLength];
            await MiscUtils.ReadExactBytesAsync(stream, encodedHeadersLength, 0,
                encodedHeadersLength.Length);
            int headersLength = MiscUtils.ParseInt32(
                MiscUtils.BytesToString(encodedHeadersLength));
            if (headersLength < 0)
            {
                throw new ChunkDecodingException(
                    "invalid length encountered for quasi http headers: " +
                    $"{headersLength}");
            }
            int maxHeadersSize = options.MaxHeadersSize;
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = QuasiHttpProtocolUtils.DefaultMaxHeadersSize;
            }
            if (headersLength > maxHeadersSize)
            {
                throw new ChunkDecodingException("quasi http headers exceed max " +
                    $"({headersLength} > {options.MaxHeadersSize})");
            }
            var headers = new byte[headersLength];
            await MiscUtils.ReadExactBytesAsync(stream, headers, 0,
                headersLength);
            return headers;
        }
    }
}
