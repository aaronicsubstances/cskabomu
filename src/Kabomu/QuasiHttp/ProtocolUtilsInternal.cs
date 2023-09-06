using Kabomu.Common;
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

        public static object CreateReaderToTransport(
            long contentLength, object body)
        {
            if (contentLength == 0)
            {
                return null;
            }
            body = IOUtils.NormalizeReader(body);
            if (contentLength < 0)
            {
                body = new ChunkEncodingCustomReader(body);
            }
            else
            {
                // don't enforce content length during writes to transport.
            }
            return body;
        }

        public static object CreateBodyFromTransport(
            long contentLength, object reader)
        {
            if (contentLength == 0)
            {
                return null;
            }
            reader = IOUtils.NormalizeReader(reader);
            if (contentLength < 0)
            {
                reader = new ChunkDecodingCustomReader(reader);
            }
            else
            {
                reader = new ContentLengthEnforcingCustomReader(reader,
                    contentLength);
            }
            return reader;
        }
    }
}
