using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Provides helper functions for writing and reading bytes to byte sources
    /// and destinations.
    /// </summary>
    public static class IOUtils
    {
        /// <summary>
        /// The limit of data buffering when reading byte streams into memory. Equal to 128 MB.
        /// </summary>
        public static readonly int DefaultDataBufferLimit = 134_217_728;

        /// <summary>
        /// The default read buffer size. Equal to 8,192 bytes.
        /// </summary>
        private static readonly int DefaultReadBufferSize = 8_192;

        /// <summary>
        /// Performs writes on behalf of an instance of <see cref="Stream"/>
        /// class or <see cref="ICustomWriter"/> interface.
        /// </summary>
        /// <param name="writer">instance of <see cref="Stream"/>
        /// class or <see cref="ICustomWriter"/> interface</param>
        /// <param name="data">destination buffer of bytes to write</param>
        /// <param name="offset">starting position in buffer to begin write from</param>
        /// <param name="length">number of bytes to write</param>
        /// <returns>a task representing end of asynchronous write operation</returns>
        /// <exception cref="ArgumentNullException">The <see cref="writer"/> argument
        /// is null</exception>
        public static Task WriteBytes(object writer,
            byte[] data, int offset, int length)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (writer is Stream s)
            {
                return s.WriteAsync(data, offset, length);
            }
            else
            {
                return ((ICustomWriter)writer).WriteBytes(data, offset, length);
            }
        }

        /// <summary>
        /// Performs reads on behalf of an instance of <see cref="Stream"/>
        /// class or <see cref="ICustomReader"/> interface.
        /// </summary>
        /// <param name="reader">instance of <see cref="Stream"/>
        /// class or <see cref="ICustomReader"/> interface</param>
        /// <param name="data">source buffer of bytes to read</param>
        /// <param name="offset">starting position in buffer to begin read from</param>
        /// <param name="length">number of bytes to read</param>
        /// <returns>a task whose result will be the number of bytes actually read, which
        /// depending of the kind of reader may be less than the number of bytes requested.</returns>
        /// <exception cref="ArgumentNullException">The <see cref="reader"/> argument
        /// is null</exception>
        public static Task<int> ReadBytes(object reader,
            byte[] data, int offset, int length)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (reader is Stream s)
            {
                return s.ReadAsync(data, offset, length);
            }
            else
            {
                return ((ICustomReader)reader).ReadBytes(data, offset, length);
            }
        }

        /// <summary>
        /// Reads in data in order to completely fill in a buffer.
        /// </summary>
        /// <param name="reader">source of bytes to read</param>
        /// <param name="data">destination buffer</param>
        /// <param name="offset">start position in buffer to fill from</param>
        /// <param name="length">number of bytes to read. Failure to obtain this number of bytes will
        /// result in an error.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        /// <exception cref="CustomIOException">Not enough bytes were found in
        /// <see cref="reader"/> argument to supply requested number of bytes.</exception>
        public static async Task ReadBytesFully(object reader,
            byte[] data, int offset, int length)
        {
            while (true)
            {
                int bytesRead = await ReadBytes(reader, data, offset, length);

                if (bytesRead < length)
                {
                    if (bytesRead <= 0)
                    {
                        throw new CustomIOException("unexpected end of read");
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
        /// Reads in all of a reader's data into an in-memory buffer.
        /// </summary>
        /// <remarks>
        /// One can specify a maximum size beyond which an error will be
        /// thrown if there is more data after that limit.
        /// </remarks>
        /// <param name="reader">The source of data to read.</param>
        /// <param name="bufferingLimit">Indicates the maximum size in bytes of the resulting buffer.
        /// Can pass zero to use default value. Can also pass a negative value which will ignore
        /// imposing a maximum size.</param>
        /// <param name="readBufferSize">The size in bytes of the read buffer.
        /// Can pass zero to use default value</param>
        /// <returns>A task whose result is an in-memory buffer which has all of the remaining data in the reader.</returns>
        /// <exception cref="CustomIOException">The <paramref name="bufferingLimit"/> argument indicates a positive value,
        /// and data in <paramref name="body"/> argument exceeded that value.</exception>
        public static async Task<byte[]> ReadAllBytes(object reader,
            int bufferingLimit = 0, int readBufferSize = 0)
        {
            if (bufferingLimit == 0)
            {
                bufferingLimit = DefaultDataBufferLimit;
            }

            var byteStream = new MemoryStream();
            if (bufferingLimit < 0)
            {
                await CopyBytes(reader, byteStream, readBufferSize);
                return byteStream.ToArray();
            }

            if (readBufferSize <= 0)
            {
                readBufferSize = DefaultReadBufferSize;
            }
            var readBuffer = new byte[readBufferSize];
            int totalBytesRead = 0;

            while (true)
            {
                int bytesToRead = Math.Min(readBuffer.Length, bufferingLimit - totalBytesRead);
                // force a read of 1 byte if there are no more bytes to read into memory stream buffer
                // but still remember that no bytes was expected.
                var expectedEndOfRead = false;
                if (bytesToRead == 0)
                {
                    bytesToRead = 1;
                    expectedEndOfRead = true;
                }
                int bytesRead = await ReadBytes(reader, readBuffer, 0, bytesToRead);
                if (bytesRead > 0)
                {
                    if (expectedEndOfRead)
                    {
                        throw CustomIOException.CreateDataBufferLimitExceededErrorMessage(
                            bufferingLimit);
                    }
                    byteStream.Write(readBuffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                }
                else
                {
                    break;
                }
            }
            return byteStream.ToArray();
        }

        /// <summary>
        /// Copies bytes from a reader to a writer.
        /// </summary>
        /// <param name="reader">source of data being transferred</param>
        /// <param name="writer">destination of data being transferred</param>
        /// <param name="readBufferSize">The size in bytes of the read buffer.
        /// Can pass zero to use default value</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        public static async Task CopyBytes(object reader, object writer, int readBufferSize = 0)
        {
            if (reader is Stream r && writer is Stream w)
            {
                if (readBufferSize <= 0)
                {
                    await r.CopyToAsync(w);
                }
                else
                {
                    await r.CopyToAsync(w, readBufferSize);
                }
                return;
            }

            if (readBufferSize <= 0)
            {
                readBufferSize = DefaultReadBufferSize;
            }
            byte[] readBuffer = new byte[readBufferSize];

            while (true)
            {
                int bytesRead = await ReadBytes(reader, readBuffer, 0, readBuffer.Length);

                if (bytesRead > 0)
                {
                    await WriteBytes(writer, readBuffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
