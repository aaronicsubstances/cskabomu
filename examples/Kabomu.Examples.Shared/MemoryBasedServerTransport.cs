using Kabomu.Common;
using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class MemoryBasedServerTransport : IQuasiHttpServerTransport
    {
        public StandardQuasiHttpServer Server { get; set; }

        public Task AcceptConnection(IConnectionAllocationResponse c)
        {
            return Server.AcceptConnection(c);
        }

        public object GetReader(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return new LambdaBasedCustomReader
            {
                ReadFunc = (data, offset, length) =>
                    typedConnection.ProcessReadRequest(true, data, offset, length)
            };
        }

        public object GetWriter(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return new LambdaBasedCustomWriter
            {
                WriteFunc = (data, offset, length) =>
                    typedConnection.ProcessWriteRequest(true, data, offset, length)
            };
        }

        public Task ReleaseConnection(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return typedConnection.Release();
        }
    }
}
