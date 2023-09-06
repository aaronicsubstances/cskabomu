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
        public static object CreateReaderToTransport(
            long contentLength, object body)
        {
            if (contentLength == 0 || body == null)
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
