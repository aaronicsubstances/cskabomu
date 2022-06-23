using Kabomu.Common;
using Kabomu.Common.Bodies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ProtocolUtils
    {
        public static int DetermineEffectiveOverallReqRespTimeoutMillis(IQuasiHttpSendOptions firstOptions,
            IQuasiHttpSendOptions fallbackOptions, int defaultValue)
        {
            if (firstOptions != null)
            {
                int effectiveValue = firstOptions.OverallReqRespTimeoutMillis;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            if (fallbackOptions != null)
            {
                int effectiveValue = fallbackOptions.OverallReqRespTimeoutMillis;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            return defaultValue;
        }

        public static int DetermineEffectiveMaxChunkSize(IQuasiHttpSendOptions firstOptions,
            IQuasiHttpSendOptions fallbackOptions, int secondFallback, int defaultValue)
        {
            if (firstOptions != null)
            {
                int effectiveValue = firstOptions.MaxChunkSize;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            if (fallbackOptions != null)
            {
                int effectiveValue = fallbackOptions.MaxChunkSize;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            return  secondFallback > 0 ? secondFallback : defaultValue;
        }

        public static void DetermineEffectiveRequestEnvironment(
            IDictionary<string, object> dest,
            IQuasiHttpSendOptions firstOptions,
            IQuasiHttpSendOptions fallbackOptions)
        {
            // since we want first options to overwrite fall back options,
            // set fall back options first.
            if (fallbackOptions?.RequestEnvironment != null)
            {
                foreach (var item in fallbackOptions.RequestEnvironment)
                {
                    dest.Add(item.Key, item.Value);
                }
            }
            if (firstOptions?.RequestEnvironment != null)
            {
                foreach (var item in firstOptions.RequestEnvironment)
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
        }

        public static async Task WriteLeadChunk(IQuasiHttpTransport transport, object connection, 
            int maxChunkSize, LeadChunk chunk)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            if (chunk == null)
            {
                throw new ArgumentException("null chunk");
            }
            var slices = chunk.Serialize();
            int byteCount = 0;
            foreach (var slice in slices)
            {
                byteCount += slice.Length;
            }
            if (byteCount > maxChunkSize)
            {
                throw new Exception($"headers larger than max chunk size of {maxChunkSize}");
            }
            if (byteCount > ChunkEncodingBody.MaxChunkSizeLimit)
            {
                throw new Exception($"headers larger than max chunk size limit of {ChunkEncodingBody.MaxChunkSizeLimit}");
            }
            var encodedLength = new byte[2];
            ByteUtils.SerializeUpToInt64BigEndian(byteCount, encodedLength, 0,
                encodedLength.Length);
            await transport.WriteBytes(connection, encodedLength, 0, encodedLength.Length);
            await TransportUtils.WriteByteSlices(transport, connection, slices);
        }
    }
}