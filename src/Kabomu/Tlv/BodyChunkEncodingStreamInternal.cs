using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Tlv
{
    /// <summary>
    /// The standard encoder of http bodies of unknown
    /// content lengths in the Kabomu library.
    /// </summary>
    /// <remarks>
    /// Receives a dest stream into which it generates
    /// an unknown number of body chunks.
    /// </remarks>
    internal class BodyChunkEncodingStreamInternal : WritableStreamBaseInternal
    {
        private readonly Stream _backingStream;
        private readonly byte[] _tagToUse;
        private static readonly byte[] EncodedZeroLength = new byte[4];

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="backingStream">the source stream</param>
        /// <param name="tagToUse"></param>
        /// <exception cref="ArgumentNullException">The <paramref name="backingStream"/> argument is null.</exception>
        public BodyChunkEncodingStreamInternal(Stream backingStream,
            int tagToUse)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            _backingStream = backingStream;
            _tagToUse = new byte[4];
            TlvUtils.EncodeTag(tagToUse, _tagToUse, 0);
        }

        public override void Flush()
        {
            _backingStream.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _backingStream.FlushAsync(cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            _backingStream.Write(_tagToUse);
            _backingStream.WriteByte(0);
            _backingStream.WriteByte(0);
            _backingStream.WriteByte(0);
            _backingStream.WriteByte(1);
            _backingStream.WriteByte(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count < 0)
            {
                _backingStream.Write(_tagToUse);
                _backingStream.Write(EncodedZeroLength);
            }
            else if (count == 0)
            {
                _backingStream.Write(buffer, offset, count);
            }
            else
            {
                var encodedLen = new byte[4];
                TlvUtils.EncodeLength(count, encodedLen, 0);
                _backingStream.Write(_tagToUse);
                _backingStream.Write(encodedLen);
                _backingStream.Write(buffer, offset, count);
            }
        }

        public override async Task WriteAsync(
            byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            if (count < 0)
            {
                await _backingStream.WriteAsync(_tagToUse);
                await _backingStream.WriteAsync(EncodedZeroLength);
            }
            else if (count == 0)
            {
                await _backingStream.WriteAsync(buffer, offset, count,
                    cancellationToken);
            }
            else
            {
                var encodedLen = new byte[4];
                TlvUtils.EncodeLength(count, encodedLen, 0);
                await _backingStream.WriteAsync(_tagToUse,
                    cancellationToken);
                await _backingStream.WriteAsync(encodedLen,
                    cancellationToken);
                await _backingStream.WriteAsync(buffer, offset, count,
                    cancellationToken);
            }
        }
    }
}
