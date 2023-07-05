using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    internal class DemoQuasiHttpTransport : IQuasiHttpTransport
    {
        private readonly MemoryStream _stream;
        private readonly byte[] _chunkMarker;
        private readonly object _expectedConnection;

        public DemoQuasiHttpTransport(object expectedConnection) :
            this(expectedConnection, null, null)
        { }

        public DemoQuasiHttpTransport(object expectedConnection,
            byte[] srcData, string chunkMarker)
        {
            _expectedConnection = expectedConnection;
            _stream = new MemoryStream();
            _chunkMarker = ByteUtils.StringToBytes(chunkMarker ?? "");

            _stream.Write(srcData ?? new byte[0]);
            // rewind for reading.
            _stream.Position = 0;
        }

        public MemoryStream BufferStream => _stream;

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            if (connection != _expectedConnection)
            {
                throw new Exception("unexpected connection");
            }
            return _stream.ReadAsync(data, offset, length);
        }

        public async Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            if (connection != _expectedConnection)
            {
                throw new Exception("unexpected connection");
            }
            await _stream.WriteAsync(data, offset, length);
            await _stream.WriteAsync(_chunkMarker);
        }

        public Task ReleaseConnection(object connection)
        {
            if (connection != _expectedConnection)
            {
                throw new Exception("unexpected connection");
            }
            _stream.Dispose();
            return Task.CompletedTask;
        }
    }
}
