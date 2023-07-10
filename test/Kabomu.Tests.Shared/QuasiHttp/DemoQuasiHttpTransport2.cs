using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    public class DemoQuasiHttpTransport2 : IQuasiHttpTransport
    {
        private readonly object _expectedConnection;
        private readonly ICustomReader _backingReader;
        private readonly ICustomWriter _backingWriter;

        public DemoQuasiHttpTransport2(object expectedConnection,
            ICustomReader backingReader, ICustomWriter backingWriter)
        {
            _expectedConnection = expectedConnection;
            _backingReader = backingReader;
            _backingWriter = backingWriter;
        }

        public CancellationTokenSource ReleaseIndicator { get; set; }

        public async Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            if (connection != _expectedConnection)
            {
                throw new ArgumentException("unexpected connection");
            }
            await Task.Yield();
            return await _backingReader.ReadBytes(data, offset, length);
        }

        public async Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            if (connection != _expectedConnection)
            {
                throw new ArgumentException("unexpected connection");
            }
            await Task.Yield();
            await _backingWriter.WriteBytes(data, offset, length);
        }

        public async Task ReleaseConnection(object connection)
        {
            if (connection != _expectedConnection)
            {
                throw new ArgumentException("unexpected connection");
            }
            ReleaseIndicator?.Cancel();
            await Task.Yield();
            await _backingReader.CustomDispose();
            await _backingWriter.CustomDispose();
        }
    }
}
