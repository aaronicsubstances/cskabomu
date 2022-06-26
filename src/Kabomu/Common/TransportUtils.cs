using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public static class TransportUtils
    {
        public static readonly int DefaultMaxChunkSize = 8_192;
        public static readonly int DefaultMaxChunkSizeLimit = 65_536;

        public static readonly string ContentTypePlainText = "text/plain";
        public static readonly string ContentTypeByteStream = "application/octet-stream";
        public static readonly string ContentTypeJson = "application/json";
        public static readonly string ContentTypeCsv = "text/csv";

        public static async Task ReadBodyBytesFully(IQuasiHttpBody body,
            byte[] data, int offset, int bytesToRead)
        {
            while (true)
            {
                int bytesRead = await body.ReadBytes(data, offset, bytesToRead);

                if (bytesRead < bytesToRead)
                {
                    if (bytesRead <= 0)
                    {
                        throw new Exception("unexpected end of read");
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

        public static async Task<byte[]> ReadBodyToEnd(IQuasiHttpBody body, int maxChunkSize)
        {
            var readBuffer = new byte[maxChunkSize];
            var byteStream = new MemoryStream();

            while (true)
            {
                int bytesRead = await body.ReadBytes(readBuffer, 0, readBuffer.Length);
                if (bytesRead > 0)
                {
                    byteStream.Write(readBuffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
            await body.EndRead(null);
            return byteStream.ToArray();
        }

        public static async Task ReadTransportBytesFully(IQuasiHttpTransport transport, object connection,
           byte[] data, int offset, int bytesToRead)
        {
            while (true)
            {
                int bytesRead = await transport.ReadBytes(connection, data, offset, bytesToRead);

                if (bytesRead < bytesToRead)
                {
                    if (bytesRead <= 0)
                    {
                        throw new Exception("unexpected end of read");
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

        public static async Task<byte[]> ReadTransportToEnd(IQuasiHttpTransport transport, object connection, int maxChunkSize)
        {
            var readBuffer = new byte[maxChunkSize];
            var byteStream = new MemoryStream();

            while (true)
            {
                int bytesRead = await transport.ReadBytes(connection, readBuffer, 0, readBuffer.Length);
                if (bytesRead > 0)
                {
                    byteStream.Write(readBuffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
            await transport.ReleaseConnection(connection);
            return byteStream.ToArray();
        }

        public static async Task TransferBodyToTransport(IQuasiHttpTransport transport,
            object connection, IQuasiHttpBody body, int maxChunkSize)
        {
            byte[] buffer = new byte[maxChunkSize];

            while (true)
            {
                int bytesRead = await body.ReadBytes(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    await transport.WriteBytes(connection, buffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
            await body.EndRead(null);
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
