using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Exceptions;
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
        /// The limit of data buffering when reading byte streams into memory. Equal to 128 MB.
        /// </summary>
        public static readonly int DefaultDataBufferLimit = 65_536 * 2 * 1024;

        /// <summary>
        /// The default read buffer size. Equal to 8,192 bytes.
        /// </summary>
        public static readonly int DefaultReadBufferSize = 8_192;

        /// <summary>
        /// The default value of max chunk size used by quasi http servers and clients. Equal to 8,192 bytes.
        /// </summary>
        public static readonly int DefaultMaxChunkSize = DefaultReadBufferSize;

        /// <summary>
        /// The maximum value of a max chunk size that can be tolerated during chunk decoding even if it
        /// exceeds the value used for sending. Equal to 65,536 bytes.
        /// </summary>
        /// <remarks>
        /// Practically this means that communicating parties can safely send chunks not exceeding 64KB without
        /// fear of rejection and without prior negotiation. Beyond 64KB however, communicating parties must have
        /// some prior negotiation (manual or automated) on max chunk sizes, or else chunks may be rejected
        /// by receivers as too large.
        /// </remarks>
        public static readonly int DefaultMaxChunkSizeLimit = 65_536;

        /// <summary>
        /// Request environment variable name of "kabomu.local_peer_endpoint" for local server endpoint.
        /// </summary>
        public static readonly string ReqEnvKeyLocalPeerEndpoint = "kabomu.local_peer_endpoint";

        /// <summary>
        /// Request environment variable name of "kabomu.remote_peer_endpoint" for remote client endpoint.
        /// </summary>
        public static readonly string ReqEnvKeyRemotePeerEndpoint = "kabomu.remote_peer_endpoint";

        /// <summary>
        /// Response environment variable of "kabomu.response_buffering_enabled" for indicating whether or not response has been bufferred already.
        /// </summary>
        public static readonly string ResEnvKeyResponseBufferingApplied = "kabomu.response_buffering_enabled";

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
                        throw new EndOfReadException("unexpected end of read");
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
                        throw new EndOfReadException("unexpected end of read");
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
        /// Reads in data in order to completely fill in a byte buffer slice.
        /// </summary>
        /// <param name="reader">source of bytes to read</param>
        /// <param name="data">destination buffer</param>
        /// <param name="offset">start position in buffer to fill from</param>
        /// <param name="bytesToRead">number of bytes to read. Failure to obtain this number of bytes will
        /// result in an exception.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public static async Task ReadBytesFully(ICustomReader reader,
            byte[] data, int offset, int bytesToRead)
        {
            while (true)
            {
                int bytesRead = await reader.ReadBytes(data, offset, bytesToRead);

                if (bytesRead < bytesToRead)
                {
                    if (bytesRead <= 0)
                    {
                        throw new EndOfReadException("unexpected end of read");
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

        public static async Task CopyBytes(ICustomReader reader,
            ICustomWriter writer, int bufferSize)
        {
            if (bufferSize <= 0)
            {
                bufferSize = DefaultReadBufferSize;
            }
            byte[] readBuffer = new byte[bufferSize];

            while (true)
            {
                int bytesRead = await reader.ReadBytes(readBuffer, 0, readBuffer.Length);

                if (bytesRead > 0)
                {
                    await writer.WriteBytes(readBuffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Reads in all of a quasi http body's data into an in-memory buffer within some maximum limit, and ends it.
        /// </summary>
        /// <remarks>
        /// One can optionally specifify a body size limit beyond which an <see cref="DataBufferLimitExceededException"/> instance will be
        /// thrown if there is more data after that limit.
        /// </remarks>
        /// <param name="body">The quasi http body to fully read.</param>
        /// <param name="bufferSize">The size in bytes of the read buffer; zero or negative uses default value</param>
        /// <param name="bufferingLimit">Indicates the maximum size in bytes of the resulting stream if positive; zero uses default value,
        /// a negative value leads to all of the body's data being read into resulting stream.</param>
        /// <returns>A promise whose result is an in-memory stream which has all of the quasi http body's data.</returns>
        /// <exception cref="DataBufferLimitExceededException">If <paramref name="bufferingLimit"/> argument has a nonnegative value,
        /// and data in <paramref name="body"/> argument exceeds that value.</exception>
        public static async Task<byte[]> ReadBodyToEnd(IQuasiHttpBody body, int bufferSize = 0,
            int bufferingLimit = 0)
        {
            if (bufferSize <= 0)
            {
                bufferSize = DefaultReadBufferSize;
            }
            if (bufferingLimit == 0)
            {
                bufferingLimit = DefaultDataBufferLimit;
            }
            var readBuffer = new byte[bufferSize];
            var byteStream = new MemoryStream();

            int totalBytesRead = 0;

            while (true)
            {
                int bytesToRead = readBuffer.Length;
                if (bufferingLimit >= 0)
                {
                    bytesToRead = Math.Min(bytesToRead, bufferingLimit - totalBytesRead);
                }
                // force a read of 1 byte if there are no more bytes to read into memory stream buffer
                // but still remember that no bytes was expected.
                var expectedEndOfRead = false;
                if (bytesToRead == 0)
                {
                    bytesToRead = 1;
                    expectedEndOfRead = true;
                }
                int bytesRead = await body.ReadBytes(readBuffer, 0, bytesToRead);
                if (bytesRead > 0)
                {
                    if (expectedEndOfRead)
                    {
                        throw new DataBufferLimitExceededException(bufferingLimit);
                    }
                    byteStream.Write(readBuffer, 0, bytesRead);
                    if (bufferingLimit >= 0)
                    {
                        totalBytesRead += bytesRead;
                    }
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
        /// Transfers all the data in a quasi http body to a connection of a quasi http
        /// transport, and ends the body.
        /// </summary>
        /// <param name="transport">transport of connection to write to</param>
        /// <param name="connection">destination connection</param>
        /// <param name="body">source body to read from</param>
        /// <param name="bufferSize">size in bytes of read buffer; zero or negative uses default value</param>
        /// <returns>A task that represents the asynchronous transfer operation.</returns>
        public static async Task TransferBodyToTransport(IQuasiHttpTransport transport,
            object connection, IQuasiHttpBody body, int bufferSize)
        {
            if (bufferSize <= 0)
            {
                bufferSize = DefaultReadBufferSize;
            }
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
