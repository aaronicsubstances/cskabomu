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
        /// The limit of data buffering when reading byte streams into memory. Equal to 128 MB.
        /// </summary>
        public static readonly int DefaultDataBufferLimit = 134_217_728;

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

        /// <summary>
        /// Copies all remaining bytes from an input stream into
        /// an output stream, and checks that if the total number of
        /// bytes being copied exceeds a certain limit.
        /// </summary>
        /// <param name="src">The source of data to read</param>
        /// <param name="dest">destination of data being transferred</param>
        /// <param name="bufferingLimit">The limit on the number of bytes to copy.
        /// Can be zero for a default value determined by
        /// <see cref="DefaultDataBufferLimit"/> to be used.</param>
        /// <param name="cancellationToken">
        /// The optional token to monitor for cancellation requests.</param>
        /// <returns>true if copying succeeded within specified limit; false
        /// if reading exceeded specified limit</returns>
        public static async Task<bool> CopyBytesUpToGivenLimit(
            Stream src, Stream dest,
            int bufferingLimit, CancellationToken cancellationToken)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }
            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }
            if (bufferingLimit <= 0)
            {
                bufferingLimit = DefaultDataBufferLimit;
            }
            var readBuffer = new byte[DefaultReadBufferSize];
            int totalBytesRead = 0;

            while (true)
            {
                int bytesToRead = Math.Min(readBuffer.Length, bufferingLimit - totalBytesRead);
                // force a read of 1 byte if there are no more bytes to read into memory stream buffer
                // but still remember that no bytes was expected.
                var expectedEndOfRead = false;
                if (bytesToRead <= 0)
                {
                    bytesToRead = 1;
                    expectedEndOfRead = true;
                }
                int bytesRead = await src.ReadAsync(readBuffer, 0, bytesToRead,
                    cancellationToken);
                if (bytesRead > bytesToRead)
                {
                    throw new ExpectationViolationException(
                        "read beyond requested length: " +
                        $"({bytesRead} > {bytesToRead})");
                }
                if (bytesRead > 0)
                {
                    if (expectedEndOfRead)
                    {
                        return false;
                    }
                    await dest.WriteAsync(readBuffer, 0, bytesRead,
                        cancellationToken);
                    totalBytesRead += bytesRead;
                }
                else
                {
                    break;
                }
            }
            return true;
        }
    }
}
