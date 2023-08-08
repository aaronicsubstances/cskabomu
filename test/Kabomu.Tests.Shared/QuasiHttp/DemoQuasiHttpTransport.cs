using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
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

        public object GetWriter(object connection)
        {
            if (connection != _expectedConnection)
            {
                throw new ArgumentException("unexpected connection");
            }
            return new LambdaBasedCustomWriter
            {
                WriteFunc = async (data, offset, length) =>
                {
                    await _stream.WriteAsync(data, offset, length);
                    await _stream.WriteAsync(_chunkMarker);
                }
            };
        }

        public object GetReader(object connection)
        {
            if (connection != _expectedConnection)
            {
                throw new ArgumentException("unexpected connection");
            }
            return _stream;
        }

        public Task ReleaseConnection(object connection)
        {
            if (connection != _expectedConnection)
            {
                throw new ArgumentException("unexpected connection");
            }
            _stream.Dispose();
            return Task.CompletedTask;
        }
    }
}
