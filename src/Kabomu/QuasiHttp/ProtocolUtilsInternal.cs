using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal static class ProtocolUtilsInternal
    {
        public static int DetermineEffectiveNonZeroIntegerOption(int? preferred,
            int? fallback1, int defaultValue)
        {
            if (preferred.HasValue)
            {
                int effectiveValue = preferred.Value;
                if (effectiveValue != 0)
                {
                    return effectiveValue;
                }
            }
            if (fallback1.HasValue)
            {
                int effectiveValue = fallback1.Value;
                if (effectiveValue != 0)
                {
                    return effectiveValue;
                }
            }
            return defaultValue;
        }

        public static int DetermineEffectivePositiveIntegerOption(int? preferred,
            int? fallback1, int defaultValue)
        {
            if (preferred.HasValue)
            {
                int effectiveValue = preferred.Value;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            if (fallback1.HasValue)
            {
                int effectiveValue = fallback1.Value;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            return defaultValue;
        }

        public static IDictionary<string, object> DetermineEffectiveOptions(
            IDictionary<string, object> preferred, IDictionary<string, object> fallback)
        {
            var dest = new Dictionary<string, object>();
            // since we want preferred options to overwrite fallback options,
            // set fallback options first.
            if (fallback != null)
            {
                foreach (var item in fallback)
                {
                    dest.Add(item.Key, item.Value);
                }
            }
            if (preferred != null)
            {
                foreach (var item in preferred)
                {
                    if (dest.ContainsKey(item.Key))
                    {
                        dest[item.Key] = item.Value;
                    }
                    else
                    {
                        dest.Add(item.Key, item.Value);
                    }
                }
            }
            return dest;
        }

        public static bool DetermineEffectiveBooleanOption(bool? preferred, bool? fallback1, bool defaultValue)
        {
            if (preferred.HasValue)
            {
                return preferred.Value;
            }
            return fallback1 ?? defaultValue;
        }

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

        public static async Task<IQuasiHttpBody> CreateEquivalentOfUnknownBodyInMemory(
            IQuasiHttpBody body, int bodyBufferingLimit)
        {
            // Assume that body is completely unknown,and as such has nothing
            // to do with chunk transfer protocol, or have no need for
            // content length enforcement.
            var reader = body.AsReader();

            // now read in entirety of body into memory
            var inMemBuffer = await IOUtils.ReadAllBytes(reader, bodyBufferingLimit);
            return new ByteBufferBody(inMemBuffer);
        }

        public static async Task TransferBodyToTransport(
            object writer, IQuasiHttpBody body,
            long contentLength)
        {
            if (contentLength == 0)
            {
                return;
            }
            if (contentLength < 0)
            {
                var chunkWriter = new ChunkEncodingCustomWriter(writer);
                await body.WriteBytesTo(chunkWriter);
                // important for chunked transfer to write out final empty chunk
                await chunkWriter.EndWrites();
            }
            else
            {
                await body.WriteBytesTo(writer);
            }
        }

        public static async Task<IQuasiHttpBody> CreateBodyFromTransport(
            object reader, long contentLength, Func<Task> releaseFunc,
            bool bufferingEnabled, int bodyBufferingSizeLimit)
        {
            if (contentLength == 0)
            {
                return null;
            }

            if (contentLength < 0)
            {
                reader = new ChunkDecodingCustomReader(reader);
            }
            else
            {
                reader = new ContentLengthEnforcingCustomReader(reader,
                    contentLength);
            }
            if (bufferingEnabled)
            {
                var inMemBuffer = await IOUtils.ReadAllBytes(
                    reader, bodyBufferingSizeLimit);
                return new ByteBufferBody(inMemBuffer)
                {
                    ContentLength = contentLength
                };
            }
            else
            {
                return new LambdaBasedQuasiHttpBody
                {
                    ContentLength = contentLength,
                    ReaderFunc = () => reader,
                    ReleaseFunc = releaseFunc
                };
            }
        }

        public static async Task<T> CompleteRequestProcessing<T>(
            Task<T> workTask, Task<T> timeoutTask, Task<T> cancellationTask)
        {
            if (workTask == null)
            {
                throw new ArgumentNullException(nameof(workTask));
            }

            // ignore null tasks and successful results from
            // timeout and cancellation tasks.
            var tasks = new List<Task<T>> { workTask };
            if (timeoutTask != null)
            {
                tasks.Add(timeoutTask);
            }
            if (cancellationTask != null)
            {
                tasks.Add(cancellationTask);
            }
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
            return await workTask;
        }

        public static CancellablePromiseInternal<T> CreateCancellableTimeoutTask<T>(
                int timeoutMillis,
            string timeoutMsg)
        {
            if (timeoutMillis <= 0)
            {
                return default;
            }
            var timeoutId = new CancellationTokenSource();
            var timeoutTask = Task.Delay(timeoutMillis, timeoutId.Token)
                .ContinueWith<T>(t =>
                {
                    if (!t.IsCanceled)
                    {
                        var timeoutError = new QuasiHttpRequestProcessingException(timeoutMsg,
                            QuasiHttpRequestProcessingException.ReasonCodeTimeout);
                        throw timeoutError;
                    }
                    return default;
                });
            return new CancellablePromiseInternal<T>
            {
                Task = timeoutTask,
                CancellationTokenSource = timeoutId
            };
        }
    }

    internal struct CancellablePromiseInternal<T>
    {
        public Task<T> Task { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public bool IsCancellationRequested() =>
            CancellationTokenSource?.IsCancellationRequested ?? false;

        public void Cancel()
        {
            CancellationTokenSource?.Cancel();
        }
    }
}
