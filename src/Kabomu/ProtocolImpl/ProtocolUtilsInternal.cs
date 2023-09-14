using Kabomu.Abstractions;
using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.ProtocolImpl
{
    internal static class ProtocolUtilsInternal
    {
        /// <summary>
        /// The purpose of this flag and the dead code that is present
        /// as a result, is make the dead code serve as reference for
        /// porting in stages of HTTP/1.0 only, before HTTP/1.1.
        /// </summary>
        //public static readonly bool SupportHttp10Only = false;

        public static bool? GetEnvVarAsBoolean(IDictionary<string, object> environment,
            string key)
        {
            if (environment != null && environment.ContainsKey(key))
            {
                var value = environment[key];
                if (value is bool b)
                {
                    return b;
                }
                else if (value != null)
                {
                    return bool.Parse((string)value);
                }
            }
            return null;
        }

        public static async Task WrapTimeoutTask(Task<bool> timeoutTask,
            string timeoutMsg)
        {
            if (timeoutTask == null)
            {
                return;
            }
            if (await timeoutTask)
            {
                throw new QuasiHttpException(timeoutMsg,
                    QuasiHttpException.ReasonCodeTimeout);
            }
        }

        public async static Task<T> CompleteWorkTask<T>(
            Task<T> workTask, params Task[] cancellationTasks)
        {
            if (workTask == null)
            {
                throw new ArgumentNullException(nameof(workTask));
            }

            await EliminateCancellationTasks(workTask, cancellationTasks);
            return await workTask;
        }

        public async static Task CompleteWorkTask(
            Task workTask, params Task[] cancellationTasks)
        {
            if (workTask == null)
            {
                throw new ArgumentNullException(nameof(workTask));
            }

            await EliminateCancellationTasks(workTask, cancellationTasks);
            await workTask;
        }

        private async static Task EliminateCancellationTasks(
            Task workTask, Task[] cancellationTasks)
        {
            // ignore null tasks and successful results from
            // cancellation tasks.
            var tasks = new List<Task>();
            foreach (var t in cancellationTasks)
            {
                if (t != null)
                {
                    tasks.Add(t);
                }
            }
            tasks.Add(workTask);
            while (tasks.Count > 1)
            {
                var firstTask = await Task.WhenAny(tasks);
                if (firstTask == workTask)
                {
                    break;
                }
                await firstTask; // let any exceptions bubble up.
                tasks.Remove(firstTask);
            }
        }

        public static Stream EncodeBodyToTransport(bool isResponse,
            long contentLength, Stream body)
        {
            if (contentLength == 0)
            {
                return null;
            }
            if (true) // !SupportHttp10Only
            {
                if (contentLength < 0)
                {
                    return new BodyChunkEncodingStreamInternal(body);
                }
            }
            else
            {
                if (!isResponse && contentLength < 0)
                {
                    return null;
                }
            }
            // don't enforce positive content lengths when writing out
            // quasi http bodies
            return body;
        }

        /// <summary>
        /// Determines what the effective body is that
        /// corresponds to a given content length.
        /// </summary>
        /// <param name="contentLength"></param>
        /// <param name="body"></param>
        public static Stream DecodeRequestBodyFromTransport(
            long contentLength, Stream body)
        {
            if (contentLength == 0)
            {
                return null;
            }
            if (false) // SupportHttp10Only
            {
                if (contentLength < 0)
                {
                    return null;
                }
            }
            if (body == null)
            {
                throw new QuasiHttpException("no request body");
            }
            if (true) // !SupportHttp10Only
            {
                if (contentLength < 0)
                {
                    return new BodyChunkDecodingStreamInternal(body);
                }
            }
            return new ContentLengthEnforcingStreamInternal(body, contentLength);
        }

        public static (Stream, bool, bool) DecodeResponseBodyFromTransport(
            long contentLength, Stream body,
            IDictionary<string, object> environment,
            bool? responseBufferingEnabled)
        {
            if (contentLength == 0)
            {
                return (null, false, false);
            }
            var responseStreamingEnabled = responseBufferingEnabled == false;
            if (GetEnvVarAsBoolean(
                environment, QuasiHttpUtils.EnvKeySkipResBodyDecoding) == true)
            {
                return (body, responseStreamingEnabled, false);
            }
            if (body == null)
            {
                throw new QuasiHttpException("no response body");
            }
            if (true) // !SupportHttp10Only
            {
                if (contentLength < 0)
                {
                    body = new BodyChunkDecodingStreamInternal(body);
                }
            }
            if (responseStreamingEnabled)
            {
                if (contentLength > 0)
                {
                    body = new ContentLengthEnforcingStreamInternal(body,
                        contentLength);
                }
                return (body, true, false);
            }
            return (body, false, true);
        }

        public static async Task<Stream> BufferResponseBody(
            long contentLength, Stream body, 
            int? bufferingSizeLimit,
            CancellationToken cancellationToken)
        {
            if (bufferingSizeLimit == null || bufferingSizeLimit.Value <= 0)
            {
                bufferingSizeLimit = IOUtilsInternal.DefaultDataBufferLimit;
            }
            if (contentLength < 0)
            {
                var buffered = new MemoryStream();
                bool success = await IOUtilsInternal.CopyBytesUpToGivenLimit(body,
                    buffered, bufferingSizeLimit.Value, cancellationToken);
                if (!success)
                {
                    throw new QuasiHttpException(
                        "response body of indeterminate length exceeds buffering limit of " +
                        $"{bufferingSizeLimit} bytes",
                        QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
                }
                buffered.Position = 0; // reset for reading.
                return buffered;
            }
            else
            {
                if (contentLength > bufferingSizeLimit.Value)
                {
                    throw new QuasiHttpException(
                        "response body length exceeds buffering limit " +
                        $"({contentLength} > {bufferingSizeLimit})",
                        QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
                }
                var buffer = new byte[(int)contentLength];
                await IOUtilsInternal.ReadBytesFully(body,
                    buffer, 0, buffer.Length,
                    cancellationToken);
                return new MemoryStream(buffer);
            }
        }

        public static async Task<(Stream, byte[])> ReadEntityFromTransport(
            bool isResponse, IQuasiHttpTransport transport, IQuasiHttpConnection connection)
        {
            var encodedHeadersReceiver = new List<byte[]>();
            var body = await transport.Read(connection, isResponse,
                encodedHeadersReceiver);
            // either body should be non-null or some byte chunks should be
            // present in receiver list.
            if (encodedHeadersReceiver.Count == 0)
            {
                if (body == null)
                {
                    var errMsg = isResponse ? "no response" : "no request";
                    throw new QuasiHttpException(errMsg);
                }
                await ReadEncodedHeaders(body,
                    encodedHeadersReceiver,
                    connection.ProcessingOptions?.MaxHeadersSize ?? 0,
                    connection.CancellationToken);
            }
            var encodedHeaders = MiscUtilsInternal.ConcatBuffers(encodedHeadersReceiver);
            return (body, encodedHeaders);
        }

        /// <summary>
        /// Reads as many 512-byte chunks as needed to detect
        /// the portion of a source stream representing a quasi
        /// http request or response header section.
        /// </summary>
        /// <param name="inputStream">source stream</param>
        /// <param name="encodedHeadersReceiver">list which
        /// will receive all the byte chunks to be read.</param>
        /// <param name="maxHeadersSize">limit on total
        /// size of byte chunks to be read. Can be zero in order
        /// for a default value to be used.</param>
        /// <param name="cancellationToken">
        /// The optional token to monitor for cancellation requests.</param>
        /// <returns>a task representing the asynchronous operation</returns>
        /// <exception cref="QuasiHttpException">Limit on
        /// total size of byte chunks has been reached and still
        /// end of header section has not been determined</exception>
        public static async Task ReadEncodedHeaders(Stream inputStream,
            List<byte[]> encodedHeadersReceiver, int maxHeadersSize = 0,
            CancellationToken cancellationToken = default)
        {
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = QuasiHttpUtils.DefaultMaxHeadersSize;
            }
            int totalBytesRead = 0;
            bool previousChunkEndsWithLf = false;
            bool previousChunkEndsWith2Lfs = false;
            while (true)
            {
                totalBytesRead += QuasiHttpCodec.HeaderChunkSize;
                if (totalBytesRead > maxHeadersSize)
                {
                    throw new QuasiHttpException(
                        "size of quasi http headers to read exceed " +
                        $"max size ({totalBytesRead} > {maxHeadersSize})",
                        QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
                }
                var chunk = new byte[QuasiHttpCodec.HeaderChunkSize];
                await IOUtilsInternal.ReadBytesFully(inputStream, chunk,
                    0, chunk.Length, cancellationToken);
                encodedHeadersReceiver.Add(chunk);
                byte carriageReturn = (byte)'\r', newline = (byte)'\n';
                if (previousChunkEndsWith2Lfs &&
                    (chunk[0] == carriageReturn || chunk[0] == newline))
                {
                    // done.
                    break;
                }
                if (previousChunkEndsWithLf &&
                    (chunk[0] == carriageReturn || chunk[0] == newline) &&
                    (chunk[1] == carriageReturn || chunk[1] == newline))
                {
                    // done.
                    break;
                }
                for (int i = 2; i < chunk.Length; i++)
                {
                    if (chunk[i] != carriageReturn && chunk[i] != newline)
                    {
                        continue;
                    }
                    if (chunk[i - 1] != carriageReturn && chunk[i - 1] != newline)
                    {
                        continue;
                    }
                    if (chunk[i - 2] == carriageReturn || chunk[i - 2] == newline)
                    {
                        // done.
                        // don't just break, as this will only quit
                        // the for loop and leave us in while loop.
                        return;
                    }
                }
                previousChunkEndsWithLf = chunk[^1] == carriageReturn ||
                    chunk[^1] == newline;
                previousChunkEndsWith2Lfs = previousChunkEndsWithLf &&
                    (chunk[^2] == carriageReturn || chunk[^2] == newline);
            }
        }
    }
}
