using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class DemoCustomReaderWriter : ICustomReader, ICustomWriter
    {
        private readonly MemoryStream _stream;
        private readonly byte[] _chunkMarker;

        public DemoCustomReaderWriter() :
            this(null, null)
        { }

        public DemoCustomReaderWriter(byte[] srcData) :
            this(srcData, null)
        { }

        public DemoCustomReaderWriter(byte[] srcData, string chunkMarker)
        {
            _stream = new MemoryStream();
            _chunkMarker = ByteUtils.StringToBytes(chunkMarker ?? "");

            _stream.Write(srcData ?? new byte[0]);
            // rewind for reading.
            _stream.Position = 0;
        }

        public MemoryStream BufferStream => _stream;

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            return _stream.ReadAsync(data, offset, length);
        }

        public async Task WriteBytes(byte[] data, int offset, int length)
        {
            await _stream.WriteAsync(data, offset, length);
            await _stream.WriteAsync(_chunkMarker);
        }

        public Task CustomDispose()
        {
            _stream.Dispose();
            return Task.CompletedTask;
        }
    }
}
