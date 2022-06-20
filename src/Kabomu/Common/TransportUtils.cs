using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public static class TransportUtils
    {
        public static readonly int MaxChunkSize = 65_535; // ie max unsigned 16-bit integer value.

        public static readonly string ContentTypePlainText = "text/plain";
        public static readonly string ContentTypeByteStream = "application/octet-stream";
        public static readonly string ContentTypeJson = "application/json";
        public static readonly string ContentTypeHtmlFormUrlEncoded = "application/x-www-form-urlencoded";

        public static async Task ReadBytesFully(IEventLoopApi eventLoop, IQuasiHttpBody body,
            byte[] data, int offset, int bytesToRead)
        {
            while (true)
            {
                int bytesRead = await body.ReadBytes(eventLoop, data, offset, bytesToRead);

                if (bytesRead < bytesToRead)
                {
                    if (bytesRead <= 0)
                    {
                        throw new Exception("end of quasi http body");
                    }
                    offset += bytesRead;
                    bytesToRead -= bytesRead;
                }
                else
                {
                    break;
                }
            }
        }

        public static async Task TransferBodyToTransport(IEventLoopApi eventLoop, IQuasiHttpTransport transport, 
            object connection, IQuasiHttpBody body)
        {
            int effectiveChunkSize = Math.Min(transport.MaxChunkSize, MaxChunkSize);
            byte[] buffer = new byte[effectiveChunkSize];

            while (true)
            {
                int bytesRead = await body.ReadBytes(eventLoop, buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    await transport.WriteBytes(connection, buffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
            await body.EndRead(eventLoop, null);
        }

        public static async Task<byte[]> ReadBodyToEnd(IEventLoopApi eventLoop, IQuasiHttpBody body, int maxChunkSize)
        {
            var readBuffer = new byte[maxChunkSize];
            var byteStream = new MemoryStream();

            while (true)
            {
                int bytesRead = await body.ReadBytes(eventLoop, readBuffer, 0, readBuffer.Length);
                if (bytesRead > 0)
                {
                    byteStream.Write(readBuffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
            await body.EndRead(eventLoop, null);
            return byteStream.ToArray();
        }

        public static async Task WriteByteSlices(IQuasiHttpTransport transport, object connection,
            ByteBufferSlice[] slices)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            if (slices == null)
            {
                throw new ArgumentException("null byte slices");
            }
            foreach (var slice in slices)
            {
                await transport.WriteBytes(connection, slice.Data, slice.Offset, slice.Length);
            }
        }
    }
}
