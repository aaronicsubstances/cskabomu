using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu
{
    internal static class ProtocolUtilsInternal
    {
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
            if (contentLength == 0 || body == null)
            {
                return null;
            }
            body = QuasiHttpUtils.NormalizeReader(body);
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
            reader = QuasiHttpUtils.NormalizeReader(reader);
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
