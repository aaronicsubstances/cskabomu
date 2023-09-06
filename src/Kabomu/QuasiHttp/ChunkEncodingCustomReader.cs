﻿using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public class ChunkEncodingCustomReader : ICustomReader
    {
        private readonly QuasiHttpHeadersCodec Encoder = new QuasiHttpHeadersCodec();
        private readonly object _wrappedReader;
        private bool _endOfReadSeen;

        /// <summary>
        /// Constructor for encoding chunks according to the custom
        /// chunk transfer protocol.
        /// </summary>
        /// <param name="wrappedReader">the source of bytes to encode which
        /// is acceptable by <see cref="IOUtils.ReadBytes"/> function.</param>
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
            var (bytesRead, lastChunkSeen) = await Encoder.EncodeSubsequentChunkV1(
                _wrappedReader, data, offset, length);
            if (lastChunkSeen)
            {
                _endOfReadSeen = true;
            }
            return bytesRead;
        }
    }
}
