using Kabomu.Abstractions;
using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu
{
    public class ChunkEncodingCustomReader : ICustomReader
    {
        private readonly QuasiHttpCodec Encoder = new QuasiHttpCodec();
        private readonly object _wrappedReader;
        private bool _endOfReadSeen;

        /// <summary>
        /// Constructor for encoding chunks according to the custom
        /// chunk transfer protocol.
        /// </summary>
        /// <param name="wrappedReader">the source of bytes to encode which
        /// is acceptable by <see cref="QuasiHttpUtils.ReadBytes"/> function.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedReader"/> argument is null.</exception>
        public ChunkEncodingCustomReader(object wrappedReader)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            _wrappedReader = wrappedReader;
        }

        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (_endOfReadSeen)
            {
                return 0;
            }
            try
            {
                var (bytesRead, lastChunkSeen) = await Encoder.EncodeBodyChunkV1(
                    _wrappedReader, data, offset, length);
                if (lastChunkSeen)
                {
                    _endOfReadSeen = true;
                }
                return bytesRead;
            }
            catch (Exception e)
            {
                throw new ChunkEncodingException("Failed to encode quasi http body chunk",
                    e);
            }
        }
    }
}
