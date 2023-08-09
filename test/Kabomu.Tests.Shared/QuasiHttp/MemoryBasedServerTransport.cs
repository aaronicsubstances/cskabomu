using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    public class MemoryBasedServerTransport : IQuasiHttpServerTransport
    {
        public Action<IConnectionAllocationResponse> AcceptConnectionFunc { get; set; }

        public object GetReader(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    typedConnection.ProcessReadRequest(true, data, offset, length)
            };
        }

        public object GetWriter(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return new LambdaBasedCustomReaderWriter
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
