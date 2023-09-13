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
        public static readonly bool SupportHttp10Only = false;

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
            if (SupportHttp10Only)
            {
                if (!isResponse && contentLength < 0)
                {
                    return null;
                }
            }
            else
            {
                if (contentLength < 0)
                {
                    return new BodyChunkEncodingStream(body);
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
        /// <returns></returns>
        public static Stream DecodeRequestBodyFromTransport(
            long contentLength, Stream body)
        {
            if (contentLength == 0)
            {
                return null;
            }
            if (SupportHttp10Only)
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
            if (!SupportHttp10Only)
            {
                if (contentLength < 0)
                {
                    return new BodyChunkDecodingStream(body);
                }
            }
            return new ContentLengthEnforcingStream(body, contentLength);
        }

        public static async Task<bool> DecodeResponseBodyFromTransport(
            IQuasiHttpResponse response,
            IDictionary<string, object> environment,
            IQuasiHttpProcessingOptions processingOptions,
            CancellationToken cancellationToken)
        {
            if (response.ContentLength == 0)
            {
                response.Body = null;
                return false;
            }
            var responseStreamingEnabled = processingOptions?.ResponseBufferingEnabled == false;
            if (GetEnvVarAsBoolean(
                environment, QuasiHttpCodec.EnvKeySkipResBodyDecoding) == true)
            {
                return responseStreamingEnabled;
            }
            if (response.Body == null)
            {
                throw new QuasiHttpException("no response body");
            }
            if (!SupportHttp10Only)
            {
                if (response.ContentLength < 0)
                {
                    response.Body = new BodyChunkDecodingStream(response.Body);
                }
            }
            if (responseStreamingEnabled)
            {
                if (response.ContentLength > 0)
                {
                    response.Body = new ContentLengthEnforcingStream(response.Body,
                        response.ContentLength);
                }
                return true;
            }
            int bufferingLimit = processingOptions?.ResponseBodyBufferingSizeLimit ?? 0;
            if (bufferingLimit <= 0)
            {
                bufferingLimit = MiscUtils.DefaultDataBufferLimit;
            }
            if (response.ContentLength < 0)
            {
                var buffered = new MemoryStream();
                bool success = await MiscUtils.CopyBytesUpToGivenLimit(response.Body,
                    buffered, bufferingLimit, cancellationToken);
                if (!success)
                {
                    throw new QuasiHttpException(
                        "response body of indeterminate length exceeds buffering limit of " +
                        $"{bufferingLimit} bytes",
                        QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
                }
                response.Body = buffered;
                buffered.Position = 0; // reset for reading.
            }
            else
            {
                if (response.ContentLength > bufferingLimit)
                {
                    throw new QuasiHttpException(
                        "response body length exceeds buffering limit " +
                        $"({response.ContentLength} > {bufferingLimit})",
                        QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
                }
                var buffer = new byte[(int)response.ContentLength];
                await MiscUtils.ReadBytesFully(response.Body,
                    buffer, 0, buffer.Length,
                    cancellationToken);
                response.Body = new MemoryStream(buffer);
            }
            return false;
        }
    }
}
