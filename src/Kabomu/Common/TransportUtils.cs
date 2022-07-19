using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Provides helper functions for writing and reading from quasi http transports, as well as reading from
    /// quasi http bodies.
    /// </summary>
    public static class TransportUtils
    {
        /// <summary>
        /// The default value of max chunk size used by quasi http servers and clients. Currently equal to 8,192.
        /// </summary>
        public static readonly int DefaultMaxChunkSize = 8_192;

        /// <summary>
        /// The maximum value of a max chunk size that can be tolerated during chunk decoding even if it
        /// exceeds the value used for sending. Currently equal to 65,536.
        /// </summary>
        /// <remarks>
        /// Practically this means that communicating parties can safely send chunks not exceeding 64KB without
        /// fear of rejection, without prior negotiation. Beyond 64KB however, communicating parties must have
        /// some prior negotiation (manual or automated) on max chunk sizes, or else chunks may be rejected
        /// by receivers as too large.
        /// </remarks>
        public static readonly int DefaultMaxChunkSizeLimit = 65_536;

        /// <summary>
        /// Equal to "text/plain"
        /// </summary>
        public static readonly string ContentTypePlainText = "text/plain";

        /// <summary>
        /// Equal to "application/octet-stream"
        /// </summary>
        public static readonly string ContentTypeByteStream = "application/octet-stream";

        /// <summary>
        /// Equal to "application/json"
        /// </summary>
        public static readonly string ContentTypeJson = "application/json";

        /// <summary>
        /// Equal to "text/csv"
        /// </summary>
        public static readonly string ContentTypeCsv = "text/csv";

        /// <summary>
        /// Reads in data from a quasi http body in order to completely fill in a byte buffer slice.
        /// </summary>
        /// <param name="body">source of bytes to read</param>
        /// <param name="data">destination buffer</param>
        /// <param name="offset">start position in buffer to fill from</param>
        /// <param name="bytesToRead">number of bytes to read. Failure to obtain this number of bytes will
        /// result in an exception.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
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

        /// <summary>
        /// Reads all of a quasi http body's data into memory and ends it.
        /// </summary>
        /// <param name="body">quasi http body whose data is to be read.</param>
        /// <param name="bufferSize">the size in bytes of the read buffer.</param>
        /// <returns>A promise whose result is a byte array containing all
        /// the data in the quasi http body.</returns>
        public static async Task<byte[]> ReadBodyToEnd(IQuasiHttpBody body, int bufferSize)
        {
            var readBuffer = new byte[bufferSize];
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
            await body.EndRead();
            return byteStream.ToArray();
        }

        /// <summary>
        /// Reads in data from a quasi http transport connection in order to completely fill in a byte buffer slice.
        /// </summary>
        /// <param name="transport">quasi http transport of connection to read from</param>
        /// <param name="connection">connection to read from</param>
        /// <param name="data">destination buffer</param>
        /// <param name="offset">start position in buffer to fill from</param>
        /// <param name="bytesToRead">number of bytes to read. Failure to obtain this number of bytes will
        /// result in an exception.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
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

        /// <summary>
        /// Reads all of a quasi http transport connection's data into memory and releases it.
        /// </summary>
        /// <param name="transport">quasi http transport of connection to read from</param>
        /// <param name="connection">connection to read from</param>
        /// <param name="bufferSize">the size in bytes of the read buffer.</param>
        /// <returns>A promise whose result is a byte array containing all
        /// the data in the quasi http transport connection.</returns>
        public static async Task<byte[]> ReadTransportToEnd(IQuasiHttpTransport transport, object connection, int bufferSize)
        {
            var readBuffer = new byte[bufferSize];
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

        /// <summary>
        /// Transfers all the data in a quasi http body to a connection of a quasi http
        /// transport, and ends the body.
        /// </summary>
        /// <param name="transport">transport of connection to write to</param>
        /// <param name="connection">destination connection</param>
        /// <param name="body">source body to read from</param>
        /// <param name="bufferSize">size in bytes of read buffer</param>
        /// <returns>A task that represents the asynchronous transfer operation.</returns>
        public static async Task TransferBodyToTransport(IQuasiHttpTransport transport,
            object connection, IQuasiHttpBody body, int bufferSize)
        {
            byte[] readBuffer = new byte[bufferSize];

            while (true)
            {
                int bytesRead = await body.ReadBytes(readBuffer, 0, readBuffer.Length);

                if (bytesRead > 0)
                {
                    await transport.WriteBytes(connection, readBuffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
            await body.EndRead();
        }

        /// <summary>
        /// Helper function for writing to connections of quasi http transport when the data
        /// to be written is in the form of byte buffer slices.
        /// </summary>
        /// <param name="transport">transport of connection to write to</param>
        /// <param name="connection">destination connection</param>
        /// <param name="slices">source of bytes to be written</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public static async Task WriteByteSlices(IQuasiHttpTransport transport, object connection,
            ByteBufferSlice[] slices)
        {
            foreach (var slice in slices)
            {
                await transport.WriteBytes(connection, slice.Data, slice.Offset, slice.Length);
            }
        }
    }
}
