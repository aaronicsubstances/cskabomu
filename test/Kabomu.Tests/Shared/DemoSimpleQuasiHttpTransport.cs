using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class DemoSimpleQuasiHttpTransport : IQuasiHttpTransport
    {
        private readonly object _expectedConnection;
        private readonly ICustomReader _reader;
        private readonly DemoSimpleCustomWriter _writer;

        public DemoSimpleQuasiHttpTransport(object expectedConnection,
            byte[] srcData, string chunkMarker)
        {
            _expectedConnection = expectedConnection;
            _reader = new DemoCustomReaderWritable(srcData)
            {
                TurnOffRandomization = true
            };
            _writer = new DemoSimpleCustomWriter
            {
                ChunkMarker = chunkMarker
            };
        }

        public StringBuilder Buffer => _writer.Buffer;

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            if (connection != _expectedConnection)
            {
                throw new Exception("unexpected connection");
            }
            return _reader.ReadBytes(data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            if (connection != _expectedConnection)
            {
                throw new Exception("unexpected connection");
            }
            return _writer.WriteBytes(data, offset, length);
        }

        public async Task ReleaseConnection(object connection)
        {
            await _reader.CustomDispose();
            await _writer.CustomDispose();
        }
    }
}
