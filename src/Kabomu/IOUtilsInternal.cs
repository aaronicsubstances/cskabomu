using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu
{
    /// <summary>
    /// Any wrapper of standard library function used for I/O is placed here.
    /// </summary>
    internal static class IOUtilsInternal
    {
        /// <summary>
        /// The default read buffer size. Equal to 8,192 bytes.
        /// </summary>
        public static readonly int DefaultReadBufferSize = 8_192;

        /// <summary>
        /// Reads in data asynchronously in order to completely fill in a buffer.
        /// </summary>
        /// <param name="inputStream">source of bytes to read</param>
        /// <param name="data">destination buffer</param>
        /// <param name="offset">start position in buffer to fill from</param>
        /// <param name="length">number of bytes to read. Failure to obtain this number of bytes will
        /// result in an error.</param>
        /// <param name="cancellationToken">
        /// The optional token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        /// <exception cref="KabomuIOException">Not enough bytes were found in
        /// <paramref name="inputStream"/> argument to supply requested number of bytes.</exception>
        public static async Task ReadBytesFully(Stream inputStream,
            byte[] data, int offset, int length,
            CancellationToken cancellationToken = default)
        {
            // allow zero-byte reads to proceed to touch the
            // stream, rather than just return.
            while (true)
            {
                int bytesRead = await inputStream.ReadAsync(
                    data, offset, length,
                    cancellationToken);
                if (bytesRead > length)
                {
                    throw new ExpectationViolationException(
                        "read beyond requested length: " +
                        $"({bytesRead} > {length})");
                }
                if (bytesRead < length)
                {
                    if (bytesRead <= 0)
                    {
                        throw KabomuIOException.CreateEndOfReadError();
                    }
                    offset += bytesRead;
                    length -= bytesRead;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Reads in data synchronously in order to completely fill in a buffer.
        /// </summary>
        /// <param name="inputStream">source of bytes to read</param>
        /// <param name="data">destination buffer</param>
        /// <param name="offset">start position in buffer to fill from</param>
        /// <param name="length">number of bytes to read. Failure to obtain this number of bytes will
        /// result in an error.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        /// <exception cref="KabomuIOException">Not enough bytes were found in
        /// <paramref name="inputStream"/> argument to supply requested number of bytes.</exception>
        public static void ReadBytesFullySync(Stream inputStream,
            byte[] data, int offset, int length)
        {
            // allow zero-byte reads to proceed to touch the
            // stream, rather than just return.
            while (true)
            {
                int bytesRead = inputStream.Read(
                    data, offset, length);
                if (bytesRead > length)
                {
                    throw new ExpectationViolationException(
                        "read beyond requested length: " +
                        $"({bytesRead} > {length})");
                }
                if (bytesRead < length)
                {
                    if (bytesRead <= 0)
                    {
                        throw KabomuIOException.CreateEndOfReadError();
                    }
                    offset += bytesRead;
                    length -= bytesRead;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
