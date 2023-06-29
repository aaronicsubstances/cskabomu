﻿using Kabomu.Common;
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
            if (body.ContentLength < 0)
            {
                var inMemBuffer = await TransportUtils.ReadBodyToEnd(body, bufferSize,
                    bufferingLimit);
                return new ByteBufferBody(inMemBuffer)
                {
                    ContentLength = body.ContentLength,
                    ContentType = body.ContentType
                };
            }
            else
            {
                if (body.ContentLength > bufferingLimit)
                {
                    throw new DataBufferLimitExceededException(bufferingLimit,
                        $"content length larger than buffering limit of " +
                        $"{bufferingLimit} bytes", null);
                }
                var inMemBuffer = new byte[body.ContentLength];
                await TransportUtils.ReadBodyBytesFully(body, inMemBuffer, 0,
                    inMemBuffer.Length);
                // for identical behaviour with unknown length case, close the body.
                await body.EndRead();

                return new ByteBufferBody(inMemBuffer)
                {
                    ContentType = body.ContentType
                };
            }
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

        public static async Task StartDeserializingBody(IQuasiHttpTransport transport,
            object connection, long contentLength)
        {
            if (contentLength > 0)
            {
                var encLengthBytes = new byte[ChunkEncodingBody.LengthOfEncodedChunkLength];
                await TransportUtils.ReadTransportBytesFully(transport, connection,
                    encLengthBytes, 0, encLengthBytes.Length);
                int knownContentLengthPrefix = (int)ByteUtils.DeserializeUpToInt64BigEndian(encLengthBytes, 0,
                    encLengthBytes.Length, true);
                if (knownContentLengthPrefix != ChunkEncodingBody.DefaultValueForInvalidChunkLength)
                {
                    throw new Exception("invalid prefix for known content length");
                }
            }
        }

        public static async Task TransferBodyToTransport(IQuasiHttpTransport transport, 
            object connection, int maxChunkSize, IQuasiHttpBody body)
        {
            if (body.ContentLength < 0)
            {
                body = new ChunkEncodingBody(body, maxChunkSize);
            }
            else if (body.ContentLength > 0)
            {
                var defaultPrefixForKnownContentLength = ChunkEncodingBody.EncodedChunkLengthOfDefaultInvalidValue;
                await transport.WriteBytes(connection, defaultPrefixForKnownContentLength, 0,
                    defaultPrefixForKnownContentLength.Length);
            }
            await TransportUtils.TransferBodyToTransport(transport, connection, body, maxChunkSize);
        }
    }
}