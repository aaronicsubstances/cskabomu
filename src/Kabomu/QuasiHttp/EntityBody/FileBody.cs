using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class FileBody : IQuasiHttpBody
    {
        private readonly object _mutex = new object();
        private readonly string _filePath;
        private IQuasiHttpBody _backingBody;
        private Exception _endOfReadError;

        public FileBody(string filePath, long offset, long length)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset),
                    "cannot be negative. received: " + offset);
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length),
                    "cannot be negative. received: " + length);
            }
            _filePath = filePath;
            Offset = offset;
            Length = length;
        }

        /// <summary>
        /// Gets the starting position in the file from which reads will begin.
        /// </summary>
        public long Offset { get; }

        /// <summary>
        /// Gets the total number of bytes to yield to read requests.
        /// </summary>
        public long Length { get; }

        /// <summary>
        /// Same as the total number of bytes available for read requests. It is never negative.
        /// </summary>

        public long ContentLength => Length;

        public string ContentType { get; set; }

        /// <summary>
        /// Gets the path to the file which will serve as the source of bytes for read requests. Same as the file
        /// supplied at construction time.
        /// </summary>
        public string FilePath { get; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            Task<int> bytesReadTask;
            lock (_mutex)
            {
                if (_endOfReadError != null)
                {
                    throw _endOfReadError;
                }
                if (_backingBody == null)
                {
                    var fileStream = new FileStream(_filePath,
                        FileMode.Open, FileAccess.Read, FileShare.Read, 0, true);
                    try
                    {
                        if (Offset + Length > fileStream.Length)
                        {
                            throw new Exception(
                                $"offset/length combination ({Offset}/{Length}) exceeds file size of " +
                                $"{fileStream.Length} bytes");
                        }
                        var effectiveOffset = fileStream.Seek(Offset, SeekOrigin.Begin);
                        if (effectiveOffset != Offset)
                        {
                            throw new ExpectationViolationException("could not seek to offset " + Offset);
                        }
                    }
                    catch
                    {
                        fileStream.Close();
                        throw;
                    }
                    _backingBody = new StreamBackedBody(fileStream, ContentLength);
                }
                bytesReadTask = _backingBody.ReadBytes(data, offset, bytesToRead);
            }
            int bytesRead = await bytesReadTask;
            return bytesRead;
        }

        public async Task EndRead()
        {
            Task endReadTask = null;
            lock (_mutex)
            {
                if (_endOfReadError != null)
                {
                    return;
                }
                _endOfReadError = new EndOfReadException();
                endReadTask = _backingBody.EndRead();
            }
            if (endReadTask != null)
            {
                await endReadTask;
            }
        }
    }
}
