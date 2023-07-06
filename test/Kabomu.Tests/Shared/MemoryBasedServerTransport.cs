using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class MemoryBasedServerTransport : IQuasiHttpServerTransport
    {
        public IQuasiHttpServer Server { get; set; }

        public Task AcceptConnection(IConnectionAllocationResponse c)
        {
            return Server.AcceptConnection(c);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return typedConnection.ProcessReadRequest(true, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return typedConnection.ProcessWriteRequest(true, data, offset, length);
        }

        public Task ReleaseConnection(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return typedConnection.Release();
        }
    }
}
