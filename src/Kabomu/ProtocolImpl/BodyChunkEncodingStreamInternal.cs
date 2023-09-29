using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.ProtocolImpl
{
    /// <summary>
    /// The standard encoder of http bodies of unknown (ie negative)
    /// content lengths in the Kabomu library.
    /// </summary>
    /// <remarks>
    /// Receives a dest stream into which it generates
    /// an unknown number of one or more body chunks.
    /// Then when the <see cref="WriteTerminatingChunk"/> is called,
    /// a zero length chunk is written out and flushed.
    /// </remarks>
    internal class BodyChunkEncodingStreamInternal : WritableStreamBaseInternal
    {
        private readonly Stream _backingStream;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="backingStream">the source stream</param>
        /// <exception cref="ArgumentNullException">The <paramref name="backingStream"/> argument is null.</exception>
        public BodyChunkEncodingStreamInternal(Stream backingStream)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            _backingStream = backingStream;
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
            byte[] chunkPrefix = TlvUtils.EncodeTagAndLengthOnly(
                QuasiHttpCodec.TagForBody, 1);
            _backingStream.Write(chunkPrefix);
            _backingStream.WriteByte(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return;
            }
            byte[] chunkPrefix = TlvUtils.EncodeTagAndLengthOnly(
                QuasiHttpCodec.TagForBody, count);
            _backingStream.Write(chunkPrefix);
            _backingStream.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            if (count == 0)
            {
                return;
            }
            byte[] chunkPrefix = TlvUtils.EncodeTagAndLengthOnly(
                QuasiHttpCodec.TagForBody, count);
            await _backingStream.WriteAsync(chunkPrefix, cancellationToken);
            await _backingStream.WriteAsync(buffer, offset, count,
                cancellationToken);
        }

        public async Task WriteTerminatingChunk(CancellationToken cancellationToken = default)
        {
            byte[] lastChunk = TlvUtils.EncodeTagAndLengthOnly(
                QuasiHttpCodec.TagForBody, 0);
            await _backingStream.WriteAsync(lastChunk, cancellationToken);
            await _backingStream.FlushAsync(cancellationToken);
        }

        public void WriteTerminatingChunkSync()
        {
            byte[] lastChunk = TlvUtils.EncodeTagAndLengthOnly(
                QuasiHttpCodec.TagForBody, 0);
            _backingStream.Write(lastChunk);
            _backingStream.Flush();
        }
    }
}
