using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ProtocolUtilsInternal
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

        public static async Task<IQuasiHttpBody> CreateEquivalentInMemoryBody(
            IQuasiHttpBody body, int bufferSize, int bufferingLimit)
        {
            // read in entirety of body into memory and
            // maintain content length and content type for the sake of tests.
            if (body.ContentLength < 0)
            {
                var inMemStream = await TransportUtils.ReadBodyToMemoryStream(body, bufferSize,
                    bufferingLimit);
                return new StreamBackedBody(inMemStream, body.ContentLength)
                {
                    ContentType = body.ContentType
                };
            }
            else
            {
                if (body.ContentLength > bufferingLimit)
                {
                    throw new BodySizeLimitExceededException(bufferingLimit,
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
            IRequestProcessorInternal transfer,
            Task<IQuasiHttpResponse> workTask,
            Task<IQuasiHttpResponse> cancellationTask, string errorMessage)
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
                await transfer?.AbortWithError(abortError);
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