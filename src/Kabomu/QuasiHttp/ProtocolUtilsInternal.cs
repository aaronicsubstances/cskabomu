using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
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
                    if (bool.TryParse(value as string, out b))
                    {
                        return b;
                    }
                }
            }
            return null;
        }

        public static async Task<IQuasiHttpBody> CreateEquivalentOfUnknownBodyInMemory(
            IQuasiHttpBody body, int bodyBufferingLimit)
        {
            // Assume that body is completely unknown, such as having nothing
            // to do with chunk transfer protocol
            var reader = body.AsReader();

            // but still enforce the content length. even if zero,
            // still pass it on in order to dispose the body.
            if (body.ContentLength >= 0)
            {
                reader = new ContentLengthEnforcingCustomReader(reader,
                    body.ContentLength);
            }

            // now read in entirety of body into memory and
            var inMemBuffer = await IOUtils.ReadAllBytes(reader, bodyBufferingLimit);
            
            // finally maintain content length and content type for the sake of tests.
            return new ByteBufferBody(inMemBuffer)
            {
                ContentLength = body.ContentLength,
                ContentType = body.ContentType
            };
        }

        public static async Task TransferBodyToTransport(
            IQuasiHttpTransport transport, object connection, int maxChunkSize,
            IQuasiHttpBody body)
        {
            if (body == null || body.ContentLength == 0)
            {
                return;
            }
            ICustomWriter writer = new TransportCustomReaderWriter(transport,
                connection, false);
            if (body.ContentLength < 0)
            {
                writer = new ChunkEncodingCustomWriter(writer, maxChunkSize);
            }
            await body.WriteBytesTo(writer);

            // important for chunked transfer to write out final empty chunk
            await writer.CustomDispose();
        }

        public static async Task<IQuasiHttpBody> CreateBodyFromTransport(
            IQuasiHttpTransport transport, object connection, bool releaseConnection, int maxChunkSize,
            string contentType, long contentLength, bool bufferingEnabled,
            int bodyBufferingSizeLimit)
        {
            if (contentLength == 0)
            {
                return null;
            }

            // but for the need to release connection in response processing stage,
            // as opposed to keeping connection in request processing stage,
            // could have received the reader as a parameter.
            ICustomReader transportReader = new TransportCustomReaderWriter(
                transport, connection, releaseConnection);
            if (contentLength < 0)
            {
                transportReader = new ChunkDecodingCustomReader(transportReader,
                    maxChunkSize);
            }
            else
            {
                transportReader = new ContentLengthEnforcingCustomReader(transportReader,
                    contentLength);
            }
            if (bufferingEnabled)
            {
                var inMemBuffer = await IOUtils.ReadAllBytes(
                    transportReader, bodyBufferingSizeLimit);
                return new ByteBufferBody(inMemBuffer)
                {
                    ContentType = contentType,
                    ContentLength = contentLength
                };
            }
            else
            {
                return new CustomReaderBackedBody(transportReader)
                {
                    ContentType = contentType,
                    ContentLength = contentLength
                };
            }
        }

        public static async Task<IQuasiHttpResponse> CompleteRequestProcessing(
            Task<IQuasiHttpResponse> workTask,
            Task<IQuasiHttpResponse> cancellationTask,
            string errorMessage,
            Action<QuasiHttpRequestProcessingException> errorCallback)
        {
            try
            {
                if (cancellationTask != null)
                {
                    return await await Task.WhenAny(workTask, cancellationTask);
                }
                else
                {
                    return await workTask;
                }
            }
            catch (Exception e)
            {
                // let call to abort transfer determine whether exception is significant.
                QuasiHttpRequestProcessingException abortError;
                if (e is QuasiHttpRequestProcessingException quasiHttpError)
                {
                    abortError = quasiHttpError;
                }
                else
                {
                    abortError = new QuasiHttpRequestProcessingException(
                        QuasiHttpRequestProcessingException.ReasonCodeGeneral,
                        errorMessage, e);
                }
                errorCallback?.Invoke(abortError);
                if (cancellationTask == null)
                {
                    throw abortError;
                }
            }

            // by awaiting again for transfer cancellation, any significant error will bubble up, and
            // any insignificant error will be swallowed.
            return await cancellationTask;
        }
    }
}
