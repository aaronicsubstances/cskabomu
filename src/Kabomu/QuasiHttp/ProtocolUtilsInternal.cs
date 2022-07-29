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

        public static async Task<IQuasiHttpBody> CreateEquivalentInMemoryResponseBody(
            IQuasiHttpBody responseBody, int bufferSize, int bufferingLimit)
        {
            // read in entirety of response body into memory and
            // maintain content length and content type for the sake of tests.
            if (responseBody.ContentLength < 0)
            {
                var inMemStream = await TransportUtils.ReadBodyToMemoryStream(responseBody, bufferSize,
                    bufferingLimit);
                return new StreamBackedBody(inMemStream, responseBody.ContentLength)
                {
                    ContentType = responseBody.ContentType
                };
            }
            else
            {
                if (responseBody.ContentLength > bufferingLimit)
                {
                    throw new BodySizeLimitExceededException(bufferingLimit,
                        $"content length larger than buffering limit of " +
                        $"{bufferingLimit} bytes", null);
                }
                var inMemBuffer = new byte[responseBody.ContentLength];
                await TransportUtils.ReadBodyBytesFully(responseBody, inMemBuffer, 0,
                    inMemBuffer.Length);
                return new ByteBufferBody(inMemBuffer)
                {
                    ContentType = responseBody.ContentType
                };
            }
        }
    }
}