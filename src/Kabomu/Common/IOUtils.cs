using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Provides helper functions for writing and reading bytes to IO resources.
    /// </summary>
    public static class IOUtils
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
        /// Reads in data in order to completely fill in a buffer.
        /// </summary>
        /// <param name="reader">source of bytes to read</param>
        /// <param name="data">destination buffer</param>
        /// <param name="offset">start position in buffer to fill from</param>
        /// <param name="bytesToRead">number of bytes to read. Failure to obtain this number of bytes will
        /// result in an error.</param>
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

        /// <summary>
        /// Reads in all of a reader's data into an in-memory buffer within some maximum limit, and disposes it.
        /// </summary>
        /// <remarks>
        /// One can specify a maximum size beyond which an <see cref="DataBufferLimitExceededException"/> instance will be
        /// thrown if there is more data after that limit.
        /// </remarks>
        /// <param name="body">The source of data to read.</param>
        /// <param name="bufferingLimit">Indicates the maximum size in bytes of the resulting buffer.
        /// Can pass zero to use default value. Can also pass a negative value which will ignore
        /// imposing a maximum size.</param>
        /// <param name="readBufferSize">The size in bytes of the read buffer.
        /// Can pass zero to use default value</param>
        /// <returns>A promise whose result is an in-memory buffer which has all of the remaining data in the reader.</returns>
        /// <exception cref="DataBufferLimitExceededException">If <paramref name="bufferingLimit"/> argument has a nonnegative value,
        /// and data in <paramref name="body"/> argument exceeds that value.</exception>
        public static async Task<byte[]> ReadAllBytes(ICustomReader reader,
            int bufferingLimit = 0, int readBufferSize = 0)
        {
            if (readBufferSize <= 0)
            {
                readBufferSize = DefaultReadBufferSize;
            }
            if (bufferingLimit == 0)
            {
                bufferingLimit = DefaultDataBufferLimit;
            }
            var readBuffer = new byte[readBufferSize];
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
                int bytesRead = await reader.ReadBytes(readBuffer, 0, bytesToRead);
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
            await reader.CustomDispose();
            return byteStream.ToArray();
        }

        /// <summary>
        /// Copies bytes from a reader to a writer.
        /// </summary>
        /// <param name="reader">source of data being transferred</param>
        /// <param name="writer">destination of data being transferred</param>
        /// <param name="bufferSize">The size in bytes of the read buffer.
        /// Can pass zero to use default value</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public static async Task CopyBytes(ICustomReader reader,
            ICustomWriter writer, int bufferSize = 0)
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

        public static ICustomReader CoalesceAsReader(ICustomReader reader,
            ICustomWritable fallback)
        {
            if (reader != null)
            {
                return reader;
            }
            if (fallback is ICustomReader r)
            {
                return r;
            }
            var memoryPipe = new MemoryPipeCustomReaderWriter(fallback);
            _ = fallback.WriteBytesTo(memoryPipe);
            return memoryPipe;
        }

        public static ICustomWritable CoaleasceAsWritable(ICustomWritable writable,
            ICustomReader fallback)
        {
            if (writable != null)
            {
                return writable;
            }
            if (fallback is ICustomWritable w)
            {
                return w;
            }
            return new LambdaBasedCustomWritable(
                writer => CopyBytes(fallback, writer),
                () => fallback.CustomDispose());
        }
    }
}
