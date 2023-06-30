using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Exceptions;
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
                    return bool.Parse((string)value);
                }
            }
            return null;
        }

        public static async Task<IQuasiHttpBody> CreateEquivalentInMemoryBody(
            IQuasiHttpBody body, int bufferSize, int bufferingLimit)
        {
            // read in entirety of body into memory and
            // maintain content length and content type for the sake of tests.
            var reader = IOUtils.CoalesceAsReader(body.Reader,
                body.Writable);
            if (reader == null)
            {
                return null;
            }

            if (body.ContentLength >= 0)
            {
                reader = new ContentLengthEnforcingCustomWritable(reader,
                    body.ContentLength, 0);
            }
            var inMemBuffer = await IOUtils.ReadAllBytes(
                reader, bufferSize, bufferingLimit);
            reader = new ByteBufferCustomWritable(inMemBuffer);
            return new DefaultQuasiHttpBody
            {
                Reader = reader,
                ContentLength = body.ContentLength,
                ContentType = body.ContentType
            };
        }

        public static async Task<IQuasiHttpResponse> CompleteRequestProcessing(
            Task<IQuasiHttpResponse> workTask,
            Task<IQuasiHttpResponse> cancellationTask,
            string errorMessage,
            Action<Exception> errorCallback)
        {
            try
            {
                if (cancellationTask != null)
                {
                    await await Task.WhenAny(workTask, cancellationTask);
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

        public static IQuasiHttpResponse CloneQuasiHttpResponse(IQuasiHttpResponse response,
            Action<IQuasiHttpMutableResponse> modifier)
        {
            var resClone = new DefaultQuasiHttpResponse
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers,
                HttpVersion = response.HttpVersion,
                HttpStatusMessage = response.HttpStatusMessage,
                Body = response.Body,
                Environment = response.Environment
            };
            if (modifier != null)
            {
                modifier.Invoke(resClone);
            }
            return resClone;
        }

        public static async Task TransferBody(ICustomWriter writer, int maxChunkSize,
            IQuasiHttpBody body)
        {
            if (body == null || body.ContentLength == 0)
            {
                return;
            }
            var writable = IOUtils.CoaleasceAsWritable(body.Writable,
                body.Reader, body.ContentLength, maxChunkSize);
            if (writable == null)
            {
                if (body.ContentLength > 0)
                {
                    throw new Exception("body not provided even though content length is positive");
                }
            }
            if (body.ContentLength > 0)
            {
                await ChunkedTransferUtils.WriteHeaderForBodyWithKnownLength(writer);
            }
            else
            {
                writer = new ChunkEncodingCustomWriter(writer, maxChunkSize);
            }
            // instead of quitting when writable is null, we instead
            // want to make it possible to use chunk encoding to
            // write out an empty chunk.
            if (writable != null)
            {
                await writable.WriteBytesTo(writer);
            }
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

            ICustomReader transportReader = new TransportCustomReaderWriter(
                transport, connection, releaseConnection);
            if (contentLength > 0)
            {
                await ChunkedTransferUtils.ReadAwayHeaderForBodyWithKnownLength(transportReader);
                transportReader = new ContentLengthEnforcingCustomWritable(transportReader,
                    contentLength, maxChunkSize);
            }
            else
            {
                transportReader = new ChunkDecodingCustomReader(transportReader,
                    maxChunkSize);
            }
            if (bufferingEnabled)
            {
                var inMemBuffer = await IOUtils.ReadAllBytes(
                    transportReader, maxChunkSize, bodyBufferingSizeLimit);
                transportReader = new ByteBufferCustomWritable(inMemBuffer);
            }
            return new DefaultQuasiHttpBody
            {
                ContentType = contentType,
                ContentLength = contentLength,
                Reader = transportReader
            };
        }
    }
}